(function () {
  'use strict';
  const announce = text => { const live = document.getElementById('ui-live'); if (live) live.textContent = text; };
  const focusFirst = root => root.querySelector('input:not([type=hidden]),textarea,select,button,a[href]')?.focus();
  document.querySelectorAll('[data-open-dialog]').forEach(button => button.addEventListener('click', () => {
    const dialog = document.getElementById(button.dataset.openDialog); if (!dialog) return;
    dialog.showModal(); dialog._opener = button; focusFirst(dialog);
  }));
  document.querySelectorAll('[data-close-dialog]').forEach(button => button.addEventListener('click', () => button.closest('dialog').close()));
  document.querySelectorAll('dialog').forEach(dialog => dialog.addEventListener('close', () => dialog._opener?.focus()));
  document.querySelectorAll('[data-vietnam-date]').forEach(el => { el.textContent = new Intl.DateTimeFormat('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh', day: '2-digit', month: '2-digit', year: 'numeric' }).format(new Date()); });
  document.querySelectorAll('[data-print]').forEach(button => button.addEventListener('click', () => window.print()));
  document.querySelectorAll('[data-copy],[data-copy-url]').forEach(button => button.addEventListener('click', async () => {
    const text = button.hasAttribute('data-copy-url') ? window.location.href : button.dataset.copy;
    try { await navigator.clipboard.writeText(text); announce('Đã sao chép.'); button.textContent = 'Đã sao chép'; }
    catch { const hint = document.createElement('p'); hint.className = 'notice'; hint.textContent = 'Chọn và sao chép: ' + text; button.after(hint); }
  }));
  document.querySelectorAll('input[type=password]').forEach(input => {
    const wrap = document.createElement('div'); wrap.className = 'password-field'; input.before(wrap); wrap.append(input);
    const button = document.createElement('button'); button.type = 'button'; button.className = 'btn'; button.textContent = 'Hiện'; button.setAttribute('aria-label', 'Hiện mật khẩu'); button.setAttribute('aria-pressed', 'false');
    button.addEventListener('click', () => { const show = input.type === 'password'; input.type = show ? 'text' : 'password'; button.textContent = show ? 'Ẩn' : 'Hiện'; button.setAttribute('aria-label', show ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'); button.setAttribute('aria-pressed', String(show)); }); wrap.append(button);
  });
  document.querySelectorAll('[data-demo-email]').forEach(button => button.addEventListener('click', () => {
    const form = document.querySelector('form[data-login]'); form.elements.Email.value = button.dataset.demoEmail; form.elements.Password.value = button.dataset.demoPassword; form.elements.Email.focus(); announce('Đã điền tài khoản thử. Bấm Đăng nhập để tiếp tục.');
  }));
  document.querySelectorAll('[data-counter-max]').forEach(input => {
    const counter = document.createElement('span'); counter.className = 'character-count'; const update = () => { counter.textContent = `${input.value.length} / ${input.dataset.counterMax} ký tự`; counter.classList.toggle('character-count--limit', input.value.length > Number(input.dataset.counterMax)); }; input.after(counter); input.addEventListener('input', update); update();
  });
  document.querySelectorAll('[data-validation-summary]').forEach(summary => {
    summary.querySelectorAll('li').forEach(li => {
      const field = Array.from(summary.closest('form').querySelectorAll('[data-valmsg-for]')).find(el => el.textContent.trim() && el.textContent.trim() === li.textContent.trim());
      if (field) { const a = document.createElement('a'); a.href = '#' + field.dataset.valmsgFor.replaceAll('.', '_'); a.textContent = li.textContent; li.replaceChildren(a); }
    });
  });
  // A cumulative amount is posted in the original field name; formatting is view-only.
  document.querySelectorAll('input[data-money]').forEach(input => {
    const raw = document.createElement('input'); raw.type = 'hidden'; raw.name = input.name; raw.value = input.value; input.removeAttribute('name'); input.type = 'text'; input.inputMode = 'numeric';
    input.value = Number(raw.value || 0).toLocaleString('vi-VN'); input.after(raw);
    input.addEventListener('input', () => { const digitsBefore = input.value.slice(0,input.selectionStart).replace(/[^0-9]/g,'').length; raw.value = input.value.replace(/[^0-9]/g, ''); const amount = Number(raw.value); input.value=raw.value?amount.toLocaleString('vi-VN'):''; let caret=0,digits=0;while(caret<input.value.length&&digits<digitsBefore){if(/[0-9]/.test(input.value[caret]))digits++;caret++;}input.setSelectionRange(caret,caret);input.setCustomValidity(raw.value && amount >= Number(input.min || 0) && (!input.max || amount <= Number(input.max)) ? '' : 'Kiểm tra số tiền trong giới hạn của phiếu.'); });
    input.addEventListener('blur', () => { input.value = Number(raw.value || 0).toLocaleString('vi-VN'); });
  });
  document.querySelectorAll('textarea[data-prescription-lines]').forEach(textarea => {
    const group=document.createElement('div');group.className='prescription-editor';
    const actions=document.createElement('div');actions.className='action-row';
    const addButton=document.createElement('button');addButton.type='button';addButton.className='link-action';addButton.textContent='Thêm thuốc';
    const toggle=document.createElement('button');toggle.type='button';toggle.className='link-action';toggle.textContent='Sửa văn bản nhiều dòng';
    const rows=[];
    const sync=()=>{textarea.value=rows.map(row=>row.dirty?row.inputs.map(input=>input.value).join(' — '):row.raw).join('\n');textarea.dispatchEvent(new Event('input'));rows.forEach((row,index)=>{row.legend.textContent='Thuốc '+(index+1);row.inputs[0].required=index===0||row.inputs.some(input=>input.value.length);row.inputs[0].setCustomValidity(index===0&&textarea.value.length>6000?'Danh sách thuốc tối đa 6000 ký tự.':'');});};
    const add=raw=>{const fieldset=document.createElement('fieldset');fieldset.className='prescription-row';const legend=document.createElement('legend');legend.textContent='Thuốc '+(rows.length+1);const fields=document.createElement('div');fields.className='prescription-fields';const parts=raw.split(' — ');if(parts.length>3)parts.splice(2,parts.length-2,parts.slice(2).join(' — '));const row={element:fieldset,legend,inputs:[],raw,dirty:false};
      ['Tên thuốc','Liều và cách dùng','Số ngày'].forEach((text,index)=>{const label=document.createElement('label');label.className='form-label';label.append(document.createTextNode(text));const input=document.createElement('input');input.type='text';input.className='form-control';input.value=parts[index]||'';if(index===2)input.inputMode='numeric';input.addEventListener('input',()=>{row.dirty=true;sync();});label.append(input);fields.append(label);row.inputs.push(input);});
      const remove=document.createElement('button');remove.type='button';remove.className='link-action link-action--danger';remove.textContent='Xóa dòng';remove.addEventListener('click',()=>{const index=rows.indexOf(row);rows.splice(index,1);fieldset.remove();if(!rows.length)add('');sync();rows[Math.min(index,rows.length-1)].inputs[0].focus();});fieldset.append(legend,fields,remove);rows.push(row);group.append(fieldset);row.inputs[0].required=rows.length===1;return row;
    };
    const rebuild=()=>{rows.length=0;group.replaceChildren();textarea.value.split('\n').forEach(add);};rebuild();
    addButton.addEventListener('click',()=>add('').inputs[0].focus());
    toggle.addEventListener('click',()=>{const textMode=textarea.hidden;textarea.hidden=!textMode;group.hidden=textMode;addButton.hidden=textMode;rows.forEach(row=>row.inputs.forEach(input=>input.disabled=textMode));toggle.textContent=textMode?'Sửa theo từng thuốc':'Sửa văn bản nhiều dòng';if(textMode)textarea.focus();else{rebuild();rows[0].inputs[0].focus();}});
    textarea.before(group);actions.append(addButton,toggle);textarea.after(actions);textarea.hidden=true;
    const originalLabel=document.querySelector('label[for="'+textarea.id+'"]');if(originalLabel)originalLabel.addEventListener('click',event=>{if(textarea.hidden){event.preventDefault();rows[0].inputs[0].focus();}});
  });
  document.querySelectorAll('textarea[data-repeat-lines]').forEach(textarea => {
    const group = document.createElement('div'); group.className = 'repeat-rows'; const actions = document.createElement('div'); actions.className = 'action-row';
    const sync = () => { const inputs=Array.from(group.querySelectorAll('input'));inputs.forEach((input,index)=>input.setAttribute('aria-label','Nội dung dòng '+(index+1)));textarea.value = inputs.map(x => x.value).join('\n'); textarea.dispatchEvent(new Event('input',{bubbles:true})); };
    const add = value => { const row = document.createElement('div'); row.className = 'repeat-row'; const input = document.createElement('input'); input.className = 'form-control'; input.value = value; input.setAttribute('aria-label', 'Nội dung dòng ' + (group.children.length + 1)); input.addEventListener('input', sync); const remove = document.createElement('button'); remove.type = 'button'; remove.className = 'btn'; remove.textContent = 'Bỏ dòng'; remove.addEventListener('click', () => { row.remove(); sync(); });const up=document.createElement('button'),down=document.createElement('button');[up,down].forEach(button=>{button.type='button';button.className='btn';});up.textContent='Lên';down.textContent='Xuống';up.addEventListener('click',()=>{if(row.previousElementSibling){group.insertBefore(row,row.previousElementSibling);sync();up.focus();}});down.addEventListener('click',()=>{if(row.nextElementSibling){group.insertBefore(row.nextElementSibling,row);sync();down.focus();}}); row.append(input,up,down,remove); group.append(row); };
    textarea.value.split('\n').forEach(add); const button = document.createElement('button'); button.type = 'button'; button.className = 'btn'; button.textContent = 'Thêm dòng'; button.addEventListener('click', () => { add(''); group.lastElementChild.querySelector('input').focus(); }); actions.append(button);
    const toggle = document.createElement('button'); toggle.type = 'button'; toggle.className = 'link-action'; toggle.textContent = 'Sửa văn bản nhiều dòng'; toggle.addEventListener('click', () => { textarea.hidden = !textarea.hidden; group.hidden = !textarea.hidden; }); actions.append(toggle);
    textarea.before(group); textarea.after(actions); textarea.hidden = true;
    textarea.addEventListener('change', () => { group.replaceChildren(); textarea.value.split('\n').forEach(add); });
  });
  document.querySelectorAll('input[type=file][accept*="pdf"]').forEach(input => {
    const feedback = document.createElement('p'); feedback.className = 'field-help'; feedback.setAttribute('aria-live', 'polite'); input.after(feedback); let previewUrl;
    input.addEventListener('change', () => {
      if (previewUrl) URL.revokeObjectURL(previewUrl); feedback.replaceChildren();feedback.className='field-help'; input.setCustomValidity(''); const file = input.files[0]; if (!file) return;
      if (file.size > 5 * 1024 * 1024 || !file.name.toLowerCase().endsWith('.pdf') || (file.type && file.type !== 'application/pdf')) { const message = 'Chọn tệp PDF không quá 5 MB.'; input.setCustomValidity(message); feedback.textContent = message; feedback.className = 'field-validation-error'; return; }
      feedback.textContent = `${file.name} · ${(file.size / 1024).toFixed(0)} KB. `; const link = document.createElement('a'); previewUrl = URL.createObjectURL(file); link.href = previewUrl; link.target = '_blank'; link.rel = 'noopener'; link.textContent = 'Xem trước PDF'; link.className = 'link-action'; feedback.append(link);
    });
  });
  document.querySelectorAll('[data-schedule-preview]').forEach(form => {
    const preview = form.querySelector('[data-schedule-output]'); if (!preview) return;
    const strip=document.createElement('div');strip.className='preview-slots';strip.setAttribute('aria-label','Mốc giờ theo cấu hình ca, chưa trừ lịch đã đặt');preview.after(strip);
    const update = () => { const start = form.querySelector('[name=StartTime]')?.value; const end = form.querySelector('[name=EndTime]')?.value; const minutes = v => v.split(':').reduce((a, x, i) => a + Number(x) * (i === 0 ? 60 : 1), 0); const interval = Number(form.querySelector('[name=SlotMinutes]')?.value); const cap = Number(form.querySelector('[name=MaxPatientsPerDay]')?.value); const count = start && end && interval > 0 ? Math.max(0, Math.floor((minutes(end) - minutes(start)) / interval)) : 0; preview.textContent = `${count} khung giờ theo ca; nhận tối đa ${Math.min(cap || 0, count)} lượt theo giới hạn hiện tại. Các mốc dưới đây chưa trừ lịch đã đặt.`;strip.replaceChildren();for(let i=0;i<Math.min(count,12);i++){const value=minutes(start)+i*interval,chip=document.createElement('span');chip.textContent=String(Math.floor(value/60)).padStart(2,'0')+':'+String(value%60).padStart(2,'0');strip.append(chip);}if(count>12){const rest=document.createElement('span');rest.textContent='Còn '+(count-12)+' mốc giờ';strip.append(rest);} }; form.addEventListener('input', update); update();
  });
  const search = document.querySelector('[data-quick-search]'); if (search) document.addEventListener('keydown', e => { if (e.key === '/' && !/INPUT|TEXTAREA|SELECT/.test(document.activeElement.tagName)) { e.preventDefault(); search.focus(); } });
  document.querySelectorAll('[data-client-filter]').forEach(input => input.addEventListener('input', () => { const value = input.value.toLocaleLowerCase('vi-VN'); const rows=Array.from(document.querySelectorAll(input.dataset.clientFilter));rows.forEach(row => { const text=input.dataset.filterAttribute?row.dataset[input.dataset.filterAttribute]:row.textContent;row.hidden = !text.toLocaleLowerCase('vi-VN').includes(value); });const status=document.getElementById(input.dataset.filterStatus);if(status)status.textContent=`Hiển thị ${rows.filter(row=>!row.hidden).length} trên ${rows.length} kết quả.`; }));
  const newsForm = document.querySelector('[data-news-preview]')?.closest('form');
  if (newsForm) { const preview=newsForm.closest('[data-catalog-editor]');const update = () => { [['Title','title'],['Excerpt','excerpt'],['Content','content']].forEach(([field, target]) => { preview.querySelector('[data-preview-' + target + ']').textContent = newsForm.elements[field].value; }); }; newsForm.addEventListener('input', update); update(); }
  const packageForm=document.querySelector('form[data-package-preview]');
  if(packageForm){const root=packageForm.closest('[data-catalog-editor]'),preview=root.querySelector('[data-preview-package]');const update=()=>{root.querySelector('[data-preview-name]').textContent=packageForm.elements.Name.value||'Tên gói khám';root.querySelector('[data-preview-price]').textContent=Number(packageForm.querySelector('input[name=Price]').value||0).toLocaleString('vi-VN')+' ₫';const popular=packageForm.querySelector('input[type=checkbox][name=Popular]').checked;preview.classList.toggle('package--popular',popular);root.querySelector('[data-preview-popular]').textContent=popular?'Gói được quan tâm':'';const list=root.querySelector('[data-preview-features]');list.replaceChildren();packageForm.elements.Features.value.split('\n').filter(line=>line.trim()).forEach(line=>{const li=document.createElement('li');li.textContent=line;list.append(li);});};packageForm.addEventListener('input',update);packageForm.addEventListener('change',update);update();}
  document.querySelectorAll('[data-edit-modal]').forEach(link => link.addEventListener('click', async event => {
    if (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
    event.preventDefault();
    try {
      const response = await fetch(link.href, {credentials:'same-origin'}); if (!response.ok || response.redirected) { window.location.assign(link.href); return; }
      const page = new DOMParser().parseFromString(await response.text(), 'text/html'); const source = page.querySelector('.portal-content form[method=post]'); if (!source) { window.location.assign(link.href); return; }
      const dialog = document.createElement('dialog'); dialog.className = 'dialog-wide'; const heading = document.createElement('h2'); heading.id = 'edit-dialog-title'; heading.textContent = page.querySelector('h1')?.textContent || 'Chỉnh sửa'; dialog.setAttribute('aria-labelledby', heading.id);
      const close = document.createElement('button'); close.type = 'button'; close.className = 'btn'; close.textContent = 'Đóng'; close.addEventListener('click', () => dialog.close());
      dialog.append(heading, source, close); document.body.append(dialog); dialog.addEventListener('close', () => { dialog.remove(); link.focus(); });
      source.querySelectorAll('a').forEach(a => { if (a.textContent.trim() === 'Quay lại') a.remove(); }); source.addEventListener('submit', () => { if (source.checkValidity()) source.querySelectorAll('button:not([type=button])').forEach(b => { b.disabled = true; b.textContent = 'Đang lưu…'; }); });
      dialog.showModal(); focusFirst(source);
    } catch { window.location.assign(link.href); }
  }));
  let dialogNumber = 0;
  document.querySelectorAll('form[data-confirm]').forEach(form => {
    // Original forms remain fully usable without scripting. Move them only after enhancement.
    const trigger = document.createElement('button'); trigger.type = 'button'; trigger.className = form.querySelector('button[type=submit],button:not([type])')?.className || 'btn'; trigger.textContent = form.querySelector('button[type=submit],button:not([type])')?.textContent.trim() || 'Tiếp tục';
    const dialog = document.createElement('dialog'); const heading = document.createElement('h2'); const id = 'confirm-title-' + (++dialogNumber); heading.id = id; heading.textContent = form.dataset.confirm; dialog.setAttribute('aria-labelledby', id); const close = document.createElement('button'); close.type = 'button'; close.className = 'btn'; close.textContent = 'Quay lại'; close.addEventListener('click', () => dialog.close());
    if(form.dataset.paymentReference){trigger.textContent='Ghi nhận thu tiền';close.textContent='Hủy bỏ';}
    form.before(trigger); document.body.append(dialog); dialog.append(heading, form, close); trigger.addEventListener('click', () => { dialog._opener = trigger; dialog.showModal(); focusFirst(form); }); dialog.addEventListener('close', () => trigger.focus());
  });
  document.querySelectorAll('form[data-payment-reference]').forEach(form=>{
    const submit=form.querySelector('button:not([type=button])');const echo=document.createElement('p');echo.className='payment-confirmation';echo.setAttribute('aria-live','polite');submit.before(echo);
    const update=()=>{const amount=form.querySelector('[name=amountPaid]')?.value||'0';echo.textContent=`Ghi nhận tổng đã thu ${Number(amount).toLocaleString('vi-VN')} ₫ cho phiếu ${form.dataset.paymentReference}. Bạn kiểm tra lại số tiền?`;};form.addEventListener('input',update);update();
  });
  document.querySelectorAll('form[method=post],form[method=POST]').forEach(form => {
    form.addEventListener('submit', async event => {
      if (form.hasAttribute('data-booking-form') && form.dataset.bookingStage !== '3') return;
      if (event.defaultPrevented || !form.checkValidity() || (window.jQuery && !window.jQuery(form).valid())) return;
      if (form.dataset.saving === 'true') { event.preventDefault(); return; }
      const buttons = Array.from(form.querySelectorAll('button:not([type=button]),input[type=submit]'));
      const concurrency = form.querySelector('input[name="RowVersion"],input[name="rowVersion"]');
      const body = new FormData(form); if (event.submitter?.name) body.append(event.submitter.name, event.submitter.value);
      form.dataset.saving = 'true'; buttons.forEach(button => { button.dataset.originalText = button.textContent; button.disabled = true; button.textContent = 'Đang lưu…'; });
      if (!concurrency) return;
      event.preventDefault();
      const restore = () => { form.dataset.saving = 'false'; buttons.forEach(button => { button.disabled = false; button.textContent = button.dataset.originalText; }); };
      try {
        const response = await fetch(form.action, { method: 'POST', body, credentials: 'same-origin' });
        if (response.redirected) { window.location.assign(response.url); return; }
        const html = await response.text(); const parsed = new DOMParser().parseFromString(html, 'text/html');
        const conflict = response.status === 409; let notice = form.querySelector('.conflict-notice'); if (!notice) { notice = document.createElement('div'); notice.className = 'conflict-notice'; notice.setAttribute('role', 'alert'); notice.tabIndex = -1; form.prepend(notice); }
        const title = document.createElement('h2'); title.textContent = conflict ? 'Phiếu đã thay đổi' : 'Chưa lưu được thông tin'; const message = document.createElement('p'); message.textContent = conflict ? 'Phiếu đã được người khác cập nhật. Thông tin bạn nhập vẫn còn ở đây. Tải lại để xem phiên bản mới trước khi lưu tiếp.' : (parsed.querySelector('.validation-summary-errors')?.textContent || parsed.body.textContent || 'Hãy kiểm tra thông tin và thử lại.').trim().slice(0, 800); notice.replaceChildren(title, message);
        if (conflict) { const reload = document.createElement('a'); reload.className = 'btn'; reload.href = window.location.href; reload.textContent = 'Tải lại trang'; notice.append(reload); } notice.focus(); restore();
      } catch { restore(); let notice = form.querySelector('.conflict-notice'); if (!notice) { notice = document.createElement('div'); notice.className = 'conflict-notice'; notice.setAttribute('role','alert'); form.prepend(notice); } notice.textContent = 'Chưa kết nối được. Thông tin đã nhập vẫn được giữ. Hãy thử lưu lại.'; }
    });
  });
  // Restore submit controls when navigating back from a completed form.
  window.addEventListener('pageshow', () => document.querySelectorAll('form[data-saving=true]').forEach(form => { form.dataset.saving = 'false'; form.querySelectorAll('[data-original-text]').forEach(button => { button.disabled = false; button.textContent = button.dataset.originalText; }); }));
  document.fonts.ready.then(() => { document.documentElement.dataset.fontsReady = 'true'; });
}());
