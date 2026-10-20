(function () {
  'use strict';
  const form = document.querySelector('[data-booking-form]'); if (!form) return;
  const doctor = form.querySelector('[data-slot-doctor]'), date = form.querySelector('[data-slot-date]'), select = form.querySelector('[data-slot-select]'), grid = form.querySelector('[data-slot-grid]'), feedback = form.querySelector('#slot-feedback');
  const stages = Array.from(form.querySelectorAll('[data-booking-stage]')), steps = Array.from(document.querySelectorAll('.booking-steps li')), back = form.querySelector('[data-booking-back]'), next = form.querySelector('[data-booking-next]'), submit = form.querySelector('[data-booking-submit]'); let stage = 0, request = 0, slotsReady=false;
  const retry=document.createElement('button');retry.type='button';retry.className='btn';retry.textContent='Tải lại giờ trống';retry.hidden=true;feedback.after(retry);retry.addEventListener('click',()=>loadSlots());
  const review = () => {
    const name = form.elements.PatientName.value; const doctorName = doctor.value === '0' ? 'Chưa chọn' : doctor.selectedOptions[0].textContent; const time = `${date.value.split('-').reverse().join('/')} · ${select.value || 'Chưa chọn giờ'}`;
    document.querySelector('[data-draft-doctor]').textContent = doctorName; document.querySelector('[data-draft-time]').textContent = time; document.querySelector('[data-draft-patient]').textContent = name || 'Chưa nhập';
    const draftFee=document.querySelector('[data-draft-fee]');if(draftFee)draftFee.textContent=select.value?(select.selectedOptions[0]?.textContent.split('·')[1]?.trim()||'Chưa chọn giờ'):'Chưa chọn giờ';
    const gender={Male:'Nam',Female:'Nữ',Other:'Khác'}[form.elements.PatientGender.value];
    const dl = form.querySelector('[data-booking-review]'); dl.replaceChildren(); [['Bác sĩ',doctorName,0],['Ngày / giờ',time,1],['Phí đã chọn',draftFee?.textContent||'Chưa chọn giờ',1],['Người khám',name,2],['Tuổi / giới tính',form.elements.PatientAge.value+' tuổi · '+gender,2],['Điện thoại',form.elements.PatientPhone.value,2],['Email',form.elements.PatientEmail.value,2],['Lý do khám',form.elements.Problem.value||'Không có',2]].forEach(([label,value,target])=>{const div=document.createElement('div'),dt=document.createElement('dt'),dd=document.createElement('dd'),edit=document.createElement('button');dt.textContent=label;dd.textContent=value;edit.type='button';edit.className='link-action';edit.textContent='Sửa';edit.setAttribute('aria-label','Sửa '+label.toLocaleLowerCase('vi-VN'));edit.addEventListener('click',()=>{show(target);stages[target].querySelector('h2').tabIndex=-1;stages[target].querySelector('h2').focus();});div.append(dt,dd,edit);dl.append(div);});
  };
  const show = value => { stage = value; form.dataset.bookingStage = String(stage); stages.forEach((section, index) => { section.hidden = index !== stage; }); steps.forEach((step, index) => { if (index === stage) step.setAttribute('aria-current', 'step'); else step.removeAttribute('aria-current'); }); back.hidden = stage === 0; next.hidden = stage === 3; submit.hidden = stage !== 3;const compactNext=document.querySelector('[data-draft-next]');if(compactNext)compactNext.hidden=stage===3; review(); };
  const valid = () => {
    if (stage === 0 && Number(doctor.value) < 1) { feedback.textContent = 'Chọn một bác sĩ trước khi tiếp tục.'; form.querySelector('#booking-doctor-search').focus(); return false; }
    if(stage===1&&(!slotsReady||select.disabled)){feedback.textContent='Chưa tải xong giờ trống cho bác sĩ và ngày này. Chờ tải xong hoặc bấm Tải lại giờ trống.';date.focus();return false;}
    if (stage === 1 && (!date.value || !select.value)) { feedback.textContent = 'Chọn ngày và một khung giờ còn trống.'; (grid.querySelector('input')||date).focus(); return false; }
    for (const input of stages[stage].querySelectorAll('input,select,textarea')) { if (!input.reportValidity() || (window.jQuery && !window.jQuery(input).valid())) { input.focus(); return false; } } return true;
  };
  const drawSlots = () => {
    grid.replaceChildren(); grid.setAttribute('role','radiogroup');
    const options = Array.from(select.options).filter(option => option.value);
    for (const period of ['Sáng','Chiều']) {
      const items = options.filter(option => (Number(option.value.split(':')[0]) < 12) === (period === 'Sáng')); if (!items.length) continue;
      const heading = document.createElement('h3'); heading.textContent = period; grid.append(heading);
      items.forEach(option => { const label = document.createElement('label'); const input = document.createElement('input'); input.type = 'radio'; input.name = 'slot-choice'; input.value = option.value; input.checked = option.value === select.value; input.addEventListener('change', () => { select.value = input.value; feedback.textContent = `Đã chọn ${input.value} ngày ${date.value.split('-').reverse().join('/')}. Phí khám: ${option.textContent.split('·')[1]?.trim() || ''}`; grid.querySelector('.slot-fee').textContent='Phí khám: '+(option.textContent.split('·')[1]?.trim()||'');review(); }); label.append(input, document.createTextNode(option.value)); grid.append(label); });
    }
    const chosen = select.selectedOptions[0];const fee = document.createElement('p');fee.className='slot-fee';fee.textContent=chosen?.value?'Phí khám: '+(chosen.textContent.split('·')[1]?.trim()||''):'Chọn một giờ để xem phí khám.';grid.append(fee);
  };
  const loadSlots = async (focusAfter=false,preserveSelection=false) => {
    const current=++request,chosen=preserveSelection?select.value:'';slotsReady=false;select.disabled=true;select.replaceChildren(new Option('Chọn giờ khám',''));grid.replaceChildren();retry.hidden=true;review();
    if(Number(doctor.value)<1||!date.value){feedback.textContent='Chọn bác sĩ và ngày khám để lấy giờ còn trống.';return;}
    feedback.textContent='Đang lấy giờ còn trống…';grid.setAttribute('aria-busy','true');
    try { const response = await fetch(`/Appointment/Slots?doctorId=${encodeURIComponent(doctor.value)}&date=${encodeURIComponent(date.value)}`); if (!response.ok) throw new Error('slots'); const slots = await response.json(); if (current !== request) return; slots.forEach(slot => { const time = slot.time ?? slot.Time; const fee = slot.fee ?? slot.Fee; select.add(new Option(`${time} · ${Number(fee).toLocaleString('vi-VN')} ₫`, time)); }); if (Array.from(select.options).some(x => x.value === chosen)) select.value = chosen;slotsReady=true;drawSlots(); feedback.textContent = slots.length ? `${slots.length} giờ còn trống. Khung giờ sẽ được kiểm tra lại khi gửi.` : 'Không còn khung giờ trống trong ngày này. Thử chọn ngày khác.'; }
    catch { if (current === request) { feedback.textContent = 'Chưa tải được giờ trống. Đổi ngày hoặc thử lại, thông tin người khám vẫn được giữ.';retry.hidden=false; } }
    finally { if (current === request) { select.disabled = !slotsReady; grid.removeAttribute('aria-busy'); review();if(focusAfter){const target=grid.querySelector('input')||date;target.focus();} } }
  };
  const week = form.querySelector('[data-week-picker]');
  let weekStart = date.min;
  const iso = day => `${day.getFullYear()}-${String(day.getMonth() + 1).padStart(2, '0')}-${String(day.getDate()).padStart(2, '0')}`;
  const shift = (value, days) => { const day = new Date(value + 'T12:00:00'); day.setDate(day.getDate() + days); return iso(day); };
  const weekControls = document.createElement('div'); weekControls.className = 'action-row'; week.before(weekControls);
  const previousWeek = document.createElement('button'), nextWeek = document.createElement('button'); [previousWeek,nextWeek].forEach(button => { button.type='button'; button.className='btn'; }); previousWeek.textContent='Tuần trước'; nextWeek.textContent='Tuần sau'; weekControls.append(previousWeek,nextWeek);
  const drawWeek = () => { week.replaceChildren(); previousWeek.disabled = weekStart <= date.min; nextWeek.disabled = shift(weekStart,7) > date.max;
    for (let i=0;i<7;i++) { const value=shift(weekStart,i); if(value>date.max)break; const day=new Date(value+'T12:00:00'); const button=document.createElement('button'); button.type='button'; const weekday=document.createElement('span'); weekday.textContent=['CN','T2','T3','T4','T5','T6','T7'][day.getDay()]; const number=document.createElement('strong'); number.textContent=String(day.getDate()).padStart(2,'0'); button.append(weekday,number); button.setAttribute('aria-label',new Intl.DateTimeFormat('vi-VN',{weekday:'long',day:'2-digit',month:'2-digit'}).format(day)); button.setAttribute('aria-pressed',String(value===date.value)); button.addEventListener('click',()=>{date.value=value;drawWeek();loadSlots();});week.append(button); }
  };
  previousWeek.addEventListener('click',()=>{weekStart=shift(weekStart,-7);if(weekStart<date.min)weekStart=date.min;drawWeek();}); nextWeek.addEventListener('click',()=>{weekStart=shift(weekStart,7);drawWeek();});
  const picker=form.querySelector('[data-doctor-picker]');
  if(picker){doctor.hidden=true;form.querySelector('label[for=DoctorId]').hidden=true;const search=picker.querySelector('input[type=search]'),speciality=picker.querySelector('select'),filters=picker.querySelector('[data-doctor-filters]'),change=picker.querySelector('[data-change-doctor]'),choices=Array.from(picker.querySelectorAll('[data-doctor-id]'));let choosing=!Number(doctor.value);
    const update=()=>{filters.hidden=!choosing;change.hidden=choosing;choices.forEach(button=>{button.hidden=choosing?!(button.textContent.toLocaleLowerCase('vi-VN').includes(search.value.toLocaleLowerCase('vi-VN'))&&(!speciality.value||button.dataset.speciality===speciality.value)):button.dataset.doctorId!==doctor.value;button.setAttribute('aria-pressed',String(button.dataset.doctorId===doctor.value));});picker.querySelector('[data-doctor-empty]').hidden=choices.some(button=>!button.hidden);};
    choices.forEach(button=>button.addEventListener('click',()=>{doctor.value=button.dataset.doctorId;choosing=false;update();loadSlots();change.focus();}));change.addEventListener('click',()=>{choosing=true;update();search.focus();});search.addEventListener('input',update);speciality.addEventListener('change',update);update();
  }
  doctor.addEventListener('change',()=>loadSlots()); date.addEventListener('change', () => { weekStart=date.value;drawWeek(); loadSlots(); }); select.addEventListener('change', () => { drawSlots(); review(); }); form.addEventListener('input', review); next.addEventListener('click', () => { if (valid()) { show(stage + 1); stages[stage].querySelector('h2').tabIndex = -1; stages[stage].querySelector('h2').focus(); } }); back.addEventListener('click', () => show(stage - 1));
  form.addEventListener('submit', event => { if (stage !== 3) { event.preventDefault(); if (valid()) show(stage + 1); } });
  document.querySelector('[data-draft-next]')?.addEventListener('click',()=>next.click());
  if (form.querySelector('.validation-summary-errors')) { stage = form.querySelector('[name=DoctorId].input-validation-error') ? 0 : form.querySelector('[name=AppointmentTime].input-validation-error,[name=AppointmentDate].input-validation-error') || /khung giờ|giờ khám|lịch khám/i.test(form.querySelector('.validation-summary-errors').textContent) ? 1 : 2; }
  select.disabled=false;select.hidden=true;show(stage); drawWeek(); drawSlots(); if (Number(doctor.value) > 0) loadSlots(stage===1&&Boolean(form.querySelector('.validation-summary-errors')),true);
}());
