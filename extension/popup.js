chrome.storage.local.get({enabled:false,last:'Aguardando conexão.'},r=>{document.getElementById('enabled').checked=r.enabled;document.getElementById('status').textContent=r.last;});
document.getElementById('enabled').onchange=e=>chrome.storage.local.set({enabled:e.target.checked});
document.getElementById('open').onclick=()=>chrome.tabs.create({url:'https://www.youtube.com/'});
