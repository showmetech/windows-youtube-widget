// Lê os cartões de vídeo da página inicial visível, em ordem visual.
// Não consulta cookies, histórico, endpoints privados ou credenciais.
let sent = false, busy = false,lastSentToken=null,checking=false;
function reset(){sent=false;lastSentToken=null;setTimeout(check,1800);}
function initialFeed(){
 // Public initial HTML data: no private endpoints, credentials or cookie access.
 for(const script of document.scripts||[]){
  const text=script.textContent||'',prefix='var ytInitialData = ';
  if(!text.startsWith(prefix)||text.length>4000000)continue;
  try{
   const data=JSON.parse(text.slice(prefix.length,text.lastIndexOf('}')+1));
   const buttons=data.topbar?.desktopTopbarRenderer?.topbarButtons||[];
   if(!buttons.some(b=>b.topbarMenuButtonRenderer?.avatar?.thumbnails?.length))return [];
   const tab=data.contents?.twoColumnBrowseResultsRenderer?.tabs?.find(t=>t.tabRenderer?.selected)?.tabRenderer;
   const result=[];
   for(const item of tab?.content?.richGridRenderer?.contents||[]){
    const content=item.richItemRenderer?.content;if(!content)continue;
    const v=content.videoRenderer,lock=content.lockupViewModel;
    if(lock&&lock.contentType!=='LOCKUP_CONTENT_TYPE_VIDEO')continue;
    const id=v?.videoId||lock?.contentId,title=v?.title?.runs?.map(r=>r.text).join('')||v?.title?.simpleText||lock?.metadata?.lockupMetadataViewModel?.title?.content||'Vídeo recomendado';
    if(!/^[A-Za-z0-9_-]{11}$/.test(id||'')||v?.upcomingEventData)continue;
    if(v?.thumbnailOverlays?.some(o=>o.thumbnailOverlayTimeStatusRenderer?.style==='UPCOMING'))continue;
    if(!result.some(x=>x.id===id))result.push({id,title});if(result.length>=30)break;
   }
   return result;
  }catch{}
 }
 return [];
}
async function check(){
 if(checking)return;checking=true;try{
 let refreshToken='';try{refreshToken=(await chrome.runtime.sendMessage({type:'recommendationContext'}))?.refreshToken||'';}catch{}
 if((sent&&lastSentToken===refreshToken)||busy||location.pathname!=='/'||(document.visibilityState!=='visible'&&!/^[a-f0-9]{32}$/.test(refreshToken)))return;
 const {enabled}=await chrome.storage.local.get({enabled:false});if(!enabled)return;
 // A presença do botão de login significa que não há sessão confirmada.
 if(document.querySelector('ytd-masthead a[href*="ServiceLogin"],ytd-masthead a[href*="accounts.google.com"]'))return;
 const initial=/^[a-f0-9]{32}$/.test(refreshToken)?initialFeed():[];
 if(!document.querySelector('ytd-masthead button#avatar-btn')&&!initial.length)return;
 const cards=[...document.querySelectorAll('ytd-rich-item-renderer')].filter(c=>c.getClientRects().length).sort((a,b)=>{const x=a.getBoundingClientRect(),y=b.getBoundingClientRect();return Math.abs(x.top-y.top)>3?x.top-y.top:x.left-y.left;});
 const videos=[];
 for(const card of cards){
  if(!card.getClientRects().length||card.querySelector('ytd-ad-slot-renderer,ytd-promoted-sparkles-web-renderer,ytd-rich-shelf-renderer'))continue;
  const link=card.querySelector('a[href^="/watch?v="],a[href^="https://www.youtube.com/watch?v="]');if(!link)continue;
  const u=new URL(link.href,location.origin), id=u.searchParams.get('v');if(!/^[A-Za-z0-9_-]{11}$/.test(id||''))continue;
  const title=card.querySelector('h3 a[href*="/watch?v="],#video-title,a.ytLockupMetadataViewModelTitle,a.yt-lockup-metadata-view-model__title')?.textContent?.trim()||link.getAttribute('title')||'Vídeo recomendado';
  if(!videos.some(v=>v.id===id)&&!card.querySelector('[overlay-style="UPCOMING"]')&&!/Programado para|Scheduled for|esperando|waiting/i.test(card.textContent||''))videos.push({id,title});
  if(videos.length>=30)break;
 }
 if(!videos.length)videos.push(...initial);
 if(!videos.length)return;
 const own=document.querySelector('a[title="Seu canal"][href^="/@"],a[title="Your channel"][href^="/@"],a[title="Tu canal"][href^="/@"]');
 const account=own?new URL(own.href,location.origin).pathname.slice(1,101):'';
 busy=true;try{const result=await chrome.runtime.sendMessage({type:'recommendation',refreshToken,id:videos[0].id,title:videos[0].title,account,videos});if(result?.ok){sent=true;lastSentToken=refreshToken;}}catch{}finally{busy=false;}
 }finally{checking=false;}
}
document.addEventListener('yt-navigate-finish',reset);document.addEventListener('visibilitychange',check);
chrome.storage.onChanged.addListener((changes)=>{if(changes.enabled)reset();});setInterval(check,4000);reset();


// Background tabs may defer timers: react directly when the home feed renders.
new MutationObserver(()=>{void check();}).observe(document,{childList:true,subtree:true});
void check();
