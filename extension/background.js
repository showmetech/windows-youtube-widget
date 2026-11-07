const validId = id => typeof id === 'string' && /^[A-Za-z0-9_-]{11}$/.test(id);
const finite = (x,min,max) => typeof x==='number' && Number.isFinite(x) && x>=min && x<=max;
function validMessage(m){
 if(!m||typeof m!=='object')return false;
 if(m.type==='recommendation')return validId(m.id)&&(!m.videos||(Array.isArray(m.videos)&&m.videos.length<=30&&m.videos.every(v=>validId(v?.id))));
 if(m.type!=='handoff'||!['prepare','commit','abort'].includes(m.phase))return false;
 if(m.phase!=='prepare'&&!/^[a-f0-9]{32}$/.test(m.transferId||''))return false;
 return m.phase==='abort'||(validId(m.id)&&finite(m.seconds,0,604800)&&finite(m.volume,0,100)&&finite(m.rate,.25,4)&&['muted','playing','live'].every(k=>typeof m[k]==='boolean'));
}
const HOST = 'com.bruno.youtube_widget';
chrome.runtime.onMessage.addListener((message, sender, respond) => {
  if(sender.id!==chrome.runtime.id||!Number.isInteger(sender.tab?.id)||sender.frameId!==0)return;
  try{if(new URL(sender.url).origin!=='https://www.youtube.com')return;}catch{return;}
  if(message?.type==='recommendationContext'){respond({refreshToken:[...refreshTabs].find(([,id])=>id===sender.tab.id)?.[0]||''});return false;}
  if(!validMessage(message)){respond({ok:false,error:'Mensagem inválida.'});return false;}
  if(message?.type==='handoff'){
    const data={type:'handoff',phase:message.phase,transferId:message.transferId,id:message.id,title:String(message.title||'').slice(0,300),seconds:message.seconds,volume:message.volume,muted:message.muted,playing:message.playing,rate:message.rate,live:message.live};
    chrome.runtime.sendNativeMessage(HOST,data).then(async result=>{try{await chrome.storage.local.set({last:result?.ok?'Transferência: '+result.phase:'Não foi possível transferir.'});}catch{}respond(result);}).catch(error=>respond({ok:false,error:'Não foi possível conectar ao YouTube Widget. '+error.message}));
    return true;
  }
  if(message?.type!=='recommendation')return;
  chrome.storage.local.get({enabled:false}, async ({enabled}) => {
    if (!enabled) { respond({ok:false,reason:'disabled'}); return; }
    const requested=typeof message.refreshToken==='string'&&/^[a-f0-9]{32}$/.test(message.refreshToken)?message.refreshToken:'';
    if(requested&&refreshTabs.get(requested)!==sender.tab.id){respond({ok:false});return;}
    const data = {type:'recommendation',refreshToken:requested, id:message.id, title:String(message.title||'').slice(0,300), account:String(message.account||'').slice(0,100), videos:(Array.isArray(message.videos)?message.videos:[]).slice(0,30).map(v=>({id:v.id,title:String(v.title||'').slice(0,300)})), at:new Date().toISOString()};
    try {
      const result = await chrome.runtime.sendNativeMessage(HOST,data);
      await chrome.storage.local.set({last:result?.ok?'Enviado: '+data.title:'Não foi possível conectar ao aplicativo.'});
      respond(result);
      if(result?.ok&&requested){refreshTabs.delete(requested);const own=await chrome.tabs.get(sender.tab.id);if(!own.active)await chrome.tabs.remove(sender.tab.id);}
    } catch (error) {await chrome.storage.local.set({last:'Instale o aplicativo atualizado para conectar. '+error.message});respond({ok:false});}
  });
  return true;
});

// A native connection delivers explicit tray requests without polling browser history.
const refreshTabs=new Map();let recommendationPort=null,reconnectTimer=null,lastRefresh='';
async function subscribeRecommendations(){
 if(recommendationPort)return;
 const {enabled}=await chrome.storage.local.get({enabled:false});if(!enabled||recommendationPort)return;
 try{
  const port=chrome.runtime.connectNative(HOST);recommendationPort=port;
  port.onMessage.addListener(async message=>{
   if(message?.type!=='refreshRecommendations'||!/^[a-f0-9]{32}$/.test(message.token||'')||message.token===lastRefresh)return;
   if(!(await chrome.storage.local.get({enabled:false})).enabled)return;
   lastRefresh=message.token;
   try{const tab=await chrome.tabs.create({url:'https://www.youtube.com/',active:false});refreshTabs.set(message.token,tab.id);
    setTimeout(async()=>{if(refreshTabs.get(message.token)!==tab.id)return;refreshTabs.delete(message.token);try{const current=await chrome.tabs.get(tab.id);if(!current.active)await chrome.tabs.remove(tab.id);}catch{}},45000);
   }catch{await chrome.storage.local.set({last:'Não foi possível abrir a página de recomendações.'});}
  });
  port.onDisconnect.addListener(()=>{void chrome.runtime.lastError;recommendationPort=null;clearTimeout(reconnectTimer);reconnectTimer=setTimeout(()=>void subscribeRecommendations(),5000);});
  port.postMessage({type:'subscribeRecommendations'});
 }catch{recommendationPort=null;}
}
chrome.runtime.onStartup?.addListener(()=>void subscribeRecommendations());
chrome.runtime.onInstalled?.addListener(()=>void subscribeRecommendations());
chrome.storage.onChanged?.addListener(changes=>{if(!changes.enabled)return;if(changes.enabled.newValue)void subscribeRecommendations();else{clearTimeout(reconnectTimer);recommendationPort?.disconnect();recommendationPort=null;}});
void subscribeRecommendations();
