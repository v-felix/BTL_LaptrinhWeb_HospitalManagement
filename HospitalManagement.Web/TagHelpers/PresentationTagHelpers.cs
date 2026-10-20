using System.Globalization;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace HospitalManagement.Web.TagHelpers;

public static class PresentationLabels
{
    public static string NewsCategory(string category) => category switch
    {
        "Awards & Recognition" => "Giải thưởng và ghi nhận",
        "Community Service" => "Hoạt động cộng đồng",
        "Facility Update" => "Cơ sở vật chất",
        "Medical Innovation" => "Tiến bộ y khoa",
        "Technology Update" => "Công nghệ y tế",
        _ => category
    };
}

[HtmlTargetElement("ui-icon")]
public sealed class IconTagHelper : TagHelper
{
    public string Name { get; set; } = "file-text";
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "svg";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", "icon");
        output.Attributes.SetAttribute("aria-hidden", "true");
        output.Attributes.SetAttribute("focusable", "false");
        output.Content.SetHtmlContent($"<use href=\"/img/icons.svg#{HtmlEncoder.Default.Encode(Name)}\"></use>");
    }
}

[HtmlTargetElement("ui-status")]
public sealed class StatusTagHelper : TagHelper
{
    public string Value { get; set; } = "Pending";
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var (label, icon, css) = Value switch
        {
            "Confirmed" => ("Đã xác nhận", "check", "Confirmed"),
            "Completed" => ("Hoàn tất", "checks", "Completed"),
            "Cancelled" => ("Đã hủy", "x", "Cancelled"),
            "Paid" => ("Đã thanh toán", "check", "Confirmed"),
            "Unpaid" => ("Chưa thanh toán", "receipt", "Unpaid"),
            "Refunded" => ("Đã hoàn tiền", "receipt", "Cancelled"),
            _ => ("Chờ xác nhận", "clock", "Pending")
        };
        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", "status " + css);
        output.Content.SetHtmlContent($"<svg class=\"icon\" aria-hidden=\"true\"><use href=\"/img/icons.svg#{icon}\"></use></svg>{label}");
    }
}

[HtmlTargetElement("ui-monogram")]
public sealed class MonogramTagHelper : TagHelper
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Photo { get; set; }
    public bool Large { get; set; }
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", $"monogram monogram--tone-{Math.Abs(Id % 5)}{(Large ? " monogram--large" : "")}");
        output.Attributes.SetAttribute("aria-hidden", "true");
        if (!string.IsNullOrWhiteSpace(Photo) && Photo.StartsWith('/') && !Photo.StartsWith("//"))
            output.Content.SetHtmlContent($"<img src=\"{HtmlEncoder.Default.Encode(Photo)}\" width=\"120\" height=\"120\" alt=\"\" loading=\"lazy\" decoding=\"async\" />");
        else
        {
            var words = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(x => !new[] { "BS.", "BS", "Dr.", "Dr" }.Contains(x, StringComparer.OrdinalIgnoreCase)).ToArray();
            output.Content.SetContent(words.Length == 0 ? "" : string.Concat(words.First()[0], words.Last()[0]));
        }
    }
}

[HtmlTargetElement("ui-photo")]
public sealed class PhotoTagHelper(IFileVersionProvider fileVersionProvider) : TagHelper
{
    public string Name { get; set; } = "hero-atrium";
    public string Alt { get; set; } = "";
    public string Sizes { get; set; } = "(min-width: 900px) 60vw, 100vw";
    public bool Hero { get; set; }
    public bool Portrait { get; set; }
    public int MinWidth { get; set; }
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "picture";
        output.TagMode = TagMode.StartTagAndEndTag;
        string Asset(string suffix) => HtmlEncoder.Default.Encode(fileVersionProvider.AddFileVersionToPath("", $"/img/photo/{Name}-{suffix}"));
        var alt = HtmlEncoder.Default.Encode(Alt);
        var sizes = HtmlEncoder.Default.Encode(Sizes);
        var media = MinWidth > 0 ? $" media=\"(min-width:{MinWidth}px)\"" : "";
        var jpeg = Asset("1024.jpg");
        var fallback = MinWidth > 0 ? "data:image/svg+xml,%3Csvg xmlns=%27http://www.w3.org/2000/svg%27/%3E" : jpeg;
        output.Content.SetHtmlContent($"<source{media} type=\"image/webp\" srcset=\"{Asset("640.webp")} 640w, {Asset("1024.webp")} 1024w, {Asset("1536.webp")} 1536w\" sizes=\"{sizes}\" /><source{media} type=\"image/jpeg\" srcset=\"{jpeg}\" /><img src=\"{fallback}\" width=\"{(Portrait ? 1024 : 1536)}\" height=\"{(Portrait ? 1536 : Name == "lab-bench" ? 658 : 1024)}\" alt=\"{alt}\" {(Hero ? "fetchpriority=\"high\"" : "loading=\"lazy\"")} decoding=\"async\" />");
    }
}

[HtmlTargetElement("label", Attributes = "asp-for")]
public sealed class RequiredLabelTagHelper : TagHelper
{
    [HtmlAttributeName("asp-for")] public ModelExpression For { get; set; } = null!;
    public override int Order => 1000;
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var content = await output.GetChildContentAsync();
        if (For.Metadata.IsRequired && For.Metadata.ModelType != typeof(bool))
            if (!content.GetContent().Contains("bắt buộc")) output.PostContent.Append(" (bắt buộc)");
    }
}

[HtmlTargetElement("table")]
public sealed class LedgerTableTagHelper : TagHelper
{
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var html = (await output.GetChildContentAsync()).GetContent();
        var regexOptions = System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase;
        var head = System.Text.RegularExpressions.Regex.Match(html, "<thead[^>]*>(.*?)</thead>", regexOptions).Groups[1].Value;
        var labels = System.Text.RegularExpressions.Regex.Matches(head, "<th\\b[^>]*>(.*?)</th>", regexOptions)
            .Select(m => System.Text.RegularExpressions.Regex.Replace(m.Groups[1].Value, "<[^>]+>", "").Trim()).ToArray();
        html = System.Text.RegularExpressions.Regex.Replace(html, "<tbody([^>]*)>(.*?)</tbody>", body =>
            "<tbody" + body.Groups[1].Value + ">" + System.Text.RegularExpressions.Regex.Replace(body.Groups[2].Value, "<tr([^>]*)>(.*?)</tr>", row =>
            {
                var index = 0;
                var content = System.Text.RegularExpressions.Regex.Replace(row.Groups[2].Value, "<(td|th)\\b([^>]*)>", cell =>
                {
                    var label = index < labels.Length ? labels[index] : "Thao tác"; index++;
                    return "<" + cell.Groups[1].Value + (cell.Groups[2].Value.Contains("data-label") ? cell.Groups[2].Value : cell.Groups[2].Value + " data-label=\"" + HtmlEncoder.Default.Encode(label) + "\"") + ">";
                }, regexOptions);
                return "<tr" + row.Groups[1].Value + ">" + content + "</tr>";
            }, regexOptions) + "</tbody>", regexOptions);
        html = System.Text.RegularExpressions.Regex.Replace(html, "<th\\b(?![^>]*scope=)([^>]*)>", "<th scope=\"col\"$1>", regexOptions);
        output.Attributes.SetAttribute("class", (output.Attributes["class"]?.Value?.ToString() ?? "table") + " ledger-responsive");
        output.Content.SetHtmlContent(html);
    }
}

[HtmlTargetElement("input", Attributes = "asp-for")]
[HtmlTargetElement("select", Attributes = "asp-for")]
[HtmlTargetElement("textarea", Attributes = "asp-for")]
public sealed class AccessibleFieldTagHelper : TagHelper
{
    [HtmlAttributeName("asp-for")] public ModelExpression For { get; set; } = null!;
    public override int Order => 1000;
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (output.Attributes["type"]?.Value?.ToString() == "hidden") return;
        var id = output.Attributes["id"]?.Value?.ToString() ?? For.Name.Replace('.', '_');
        output.Attributes.SetAttribute("aria-describedby", id + "-error");
        if (output.Attributes["class"]?.Value?.ToString()?.Contains("input-validation-error") == true)
            output.Attributes.SetAttribute("aria-invalid", "true");
        var name = For.Name;
        string? autocomplete = name switch
        {
            "FullName" or "PatientName" => "name",
            "Phone" or "PatientPhone" => "tel",
            "Email" or "PatientEmail" => "email",
            "CurrentPassword" => "current-password",
            "NewPassword" or "ConfirmPassword" => "new-password",
            _ => null
        };
        if (autocomplete != null && !output.Attributes.ContainsName("autocomplete")) output.Attributes.SetAttribute("autocomplete", autocomplete);
        if (name.Contains("Phone")) output.Attributes.SetAttribute("inputmode", "tel");
        if (name == "PatientAge") output.Attributes.SetAttribute("inputmode", "numeric");
        if (output.TagName == "textarea")
        {
            var max = output.Attributes["data-val-length-max"]?.Value?.ToString();
            if (max != null) output.Attributes.SetAttribute("data-counter-max", max);
        }
    }
}

[HtmlTargetElement("span", Attributes = "asp-validation-for")]
public sealed class ValidationFieldTagHelper : TagHelper
{
    [HtmlAttributeName("asp-validation-for")] public ModelExpression For { get; set; } = null!;
    public override int Order => 1000;
    public override void Process(TagHelperContext context, TagHelperOutput output) => output.Attributes.SetAttribute("id", For.Name.Replace('.', '_') + "-error");
}

[HtmlTargetElement("div", Attributes = "asp-validation-summary")]
public sealed class ValidationSummaryTagHelper : TagHelper
{
    public override int Order => 1000;
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.Attributes.SetAttribute("role", "alert");
        output.Attributes.SetAttribute("tabindex", "-1");
        output.Attributes.SetAttribute("data-validation-summary", "");
    }
}
