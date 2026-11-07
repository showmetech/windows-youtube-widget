(() => {
 let busy=false,scheduled=false,sourceOwner=null;
 function releaseSource(){if(!sourceOwner)return;sourceOwner.video.removeEventListener('play',sourceOwner.guard);sourceOwner.root.removeEventListener('pointerdown',sourceOwner.intent);sourceOwner.root.removeEventListener('keydown',sourceOwner.intent);sourceOwner=null;}
 function ownSource(video,id,token){
  releaseSource();const owner={video,id,token,root:document.querySelector('#movie_player'),committed:false,returning:false,returnRequested:false};sourceOwner=owner;
  owner.intent=event=>{if(event.isTrusted&&((event.type==='keydown'&&[' ','k','K'].includes(event.key))||(event.type==='pointerdown'&&event.target?.closest('video,.ytp-play-button'))))owner.returnRequested=true;};owner.root.addEventListener('pointerdown',owner.intent);owner.root.addEventListener('keydown',owner.intent);
  owner.guard=async()=>{
   if(videoId()!==id){releaseSource();return;}
   video.pause();if(!owner.committed||owner.returning||!owner.returnRequested)return;owner.returnRequested=false;owner.returning=true;
   try{const result=await send({phase:'abort',transferId:token});if(!result?.ok)throw Error();releaseSource();await video.play();notify('Reprodução retomada nesta página; widget pausado.');}
   catch{owner.returning=false;notify('Não foi possível confirmar a pausa do widget. A página continua pausada para evitar áudio duplicado.');}
  };video.addEventListener('play',owner.guard);return owner;
 }
 const BUTTON='youtube-widget-windows-button';
 function videoId(){const u=new URL(location.href);return u.pathname==='/watch'&&/^[\w-]{11}$/.test(u.searchParams.get('v')||'')?u.searchParams.get('v'):null;}
 function notify(text){let toast=document.getElementById('youtube-widget-transfer-status');if(!toast){toast=document.createElement('div');toast.id='youtube-widget-transfer-status';toast.setAttribute('role','status');Object.assign(toast.style,{position:'fixed',right:'24px',bottom:'80px',zIndex:'2147483647',background:'#202020',color:'#fff',padding:'14px 18px',borderRadius:'8px',font:'14px Segoe UI,sans-serif',maxWidth:'420px',boxShadow:'0 4px 20px #0008'});(document.fullscreenElement||document.body).appendChild(toast);}toast.textContent=text;clearTimeout(toast.timer);toast.timer=setTimeout(()=>toast.remove(),7000);}
 const send=data=>chrome.runtime.sendMessage({type:'handoff',...data});
 async function transfer(button){
  if(busy)return;const id=videoId(),root=document.querySelector('#movie_player'),video=root?.querySelector('video');
  if(!id||!video||root.classList.contains('ad-showing')||root.classList.contains('ad-interrupting')){notify('Aguarde o vídeo começar e os anúncios terminarem.');return;}
  let token='',pausedHere=false,resume=false,committed=false;
  const title=document.querySelector('h1.ytd-watch-metadata yt-formatted-string,h1.title yt-formatted-string')?.textContent?.trim()||document.title.replace(/ - YouTube$/,'');
  const details=()=>({id,title,seconds:Math.max(0,video.currentTime||0),volume:Math.round(video.volume*100),muted:video.muted,playing:!video.paused&&!video.ended,rate:video.playbackRate||1,live:video.duration===Infinity});
  busy=true;button.disabled=true;button.setAttribute('aria-busy','true');notify('Preparando o vídeo no YouTube Widget…');
  try{
   const prepared=await send({phase:'prepare',...details()});token=prepared?.transferId||'';
   if(!prepared?.ok)throw new Error(prepared?.error||'O aplicativo não confirmou o recebimento.');
   if(videoId()!==id||!video.isConnected||root.querySelector('video')!==video||root.classList.contains('ad-showing'))throw new Error('O vídeo mudou durante a transferência. Tente novamente.');
   resume=!video.paused&&!video.ended;const state=details();const owner=ownSource(video,id,token);video.pause();pausedHere=true;
   if(!video.paused)throw new Error('O YouTube não confirmou a pausa nesta página.');
   const result=await send({phase:'commit',transferId:token,...state,seconds:video.currentTime,playing:resume});
   if(!result?.ok)throw new Error(result?.error||'O widget não confirmou a reprodução.');
   committed=true;owner.committed=true;if(!video.paused)video.pause();notify('Vídeo transferido para o YouTube Widget.');
  }catch(error){
   let safeToResume=!pausedHere;
   if(token){try{const aborted=await send({phase:'abort',transferId:token});safeToResume=aborted?.ok===true;}catch{}}
   if(safeToResume)releaseSource();
   if(pausedHere&&resume&&safeToResume&&videoId()===id&&video.isConnected){try{await video.play();}catch{}}
   notify((error.message||'Não foi possível transferir o vídeo.')+(pausedHere&&!safeToResume?' Confira o widget antes de retomar a página.':''));
  }finally{busy=false;button.disabled=false;button.removeAttribute('aria-busy');if(committed)button.title='Vídeo recebido pelo YouTube Widget';}
 }
 function install(){
  scheduled=false;const id=videoId();if(sourceOwner&&sourceOwner.id!==id)releaseSource();const bar=document.querySelector('#movie_player .ytp-right-controls');
  const fullscreen=bar?.querySelector('.ytp-fullscreen-button');
  const controls=fullscreen?.parentElement||bar;
  if(!id||!controls)return;
  let button=document.getElementById(BUTTON);if(button&&button.parentElement===controls){button.disabled=busy;return;}button?.remove();
  button=document.createElement('button');button.id=BUTTON;button.type='button';button.className='ytp-button';button.title='Continuar no YouTube Widget';button.setAttribute('aria-label','Continuar no YouTube Widget');button.disabled=busy;
  const svg=document.createElementNS('http://www.w3.org/2000/svg','svg');
  for(const [key,value] of Object.entries({width:'24',height:'24',viewBox:'0 0 24 24','aria-hidden':'true',focusable:'false'}))svg.setAttribute(key,value);
  // YouTube adds responsive padding to 24px SVGs; let its native CSS size this icon.
  const path=document.createElementNS('http://www.w3.org/2000/svg','path');
  path.setAttribute('fill','white');path.setAttribute('d','M4 4h7v7H4zm9 0h7v7h-7zM4 13h7v7H4zm9 0h7v7h-7z');svg.appendChild(path);button.appendChild(svg);
  button.addEventListener('click',event=>{event.preventDefault();event.stopPropagation();void transfer(button);});
  controls.insertBefore(button,fullscreen?.parentElement===controls?fullscreen:null);
 }
 function schedule(){if(!scheduled){scheduled=true;setTimeout(install,100);}}
 new MutationObserver(schedule).observe(document.documentElement,{childList:true,subtree:true});document.addEventListener('yt-navigate-finish',schedule);setInterval(install,1500);install();
})();
