# 1.7.8

- Atualizador remove a própria pasta temporária em finally, inclusive após falhas.
- Instalador remove ícones versionados obsoletos e relatórios de diagnóstico conhecidos antes de copiar a versão nova.
- Preserva dados do usuário, extensões, manifestos ativos e arquivos não reconhecidos.

# 1.7.6

- Configurações reorganizadas em Geral, Aparência, Reprodução e Biblioteca, com ícones e textos essenciais.
- Janela ajustada à área útil do monitor, independente do tamanho do player e sem rolagem de página.
- Fila e favoritos paginados para manter todos os itens acessíveis sem barras de rolagem.
- Mantém a opacidade padrão em 100% e as preferências existentes.

# 1.7.5

- A opção padrão de aparência agora é Opaco · 100%, no widget e nas configurações.
- Mantém as opções de transparência suave e intensa para escolha manual.
- Verifica opacidade total nos temas claro, escuro e do sistema.

# 1.7.4

- Remove o cabeçalho do widget: o vídeo começa no topo, sem o nome YouTube.
- Agrupa configurações, fixar e tamanho à esquerda da base; reprodução centralizada; som e recolher à direita.
- Reduz a altura e verifica alinhamento sem sobreposição nos dois tamanhos e com escala de texto de até 200%.
- Mantém avisos de reprodução em uma linha temporária, sem título permanente.

YouTube Widget 1.7.3

- Play now requests an explicit playback state, waits for actual video progress, and retries a missed command up to three times. Requests during player loading wait for readiness instead of silently doing nothing.
- Hiding, changing videos, or an error cancels pending playback, preventing delayed playback after closing the widget. Playback failures produce visible feedback.
- Allows only media autoplay requests from the embedded app/YouTube origins, without saving permissions in the profile. Other permission kinds remain denied.
- Added real unmuted playback checks and a lost-first-command fault-injection regression. Native mouse retesting was interrupted by Windows access denial; simulated failure coverage does not prove its underlying cause.

- Restores the app-folder native-host registration for unpackaged Windows installations after a reproduced Chrome "host not found" failure. Paused and playing transfers were verified against Chrome; the page pauses before the widget starts.
- MSIX keeps its declared shared AppData registration; Microsoft Store certification is still pending.

- Handles GitHub checksum downloads correctly when PowerShell returns binary response content. Installer hashes are always verified before execution.

- Self-contained x64, x86 and ARM64 distributions, with the official Microsoft WebView2 runtime included.
- Matching application/tray/desktop icons and startup in the notification area.
- Refreshes Chrome/Edge native-host registration on first launch and after updates.
- MSIX packaging uses Windows startup tasks and declares the native-host interoperability it requires.
- Repeatable build, test, GitHub release and gated Microsoft Store submission workflow.

Microsoft Store publication is blocked while the existing Showmetech company account is under Microsoft business verification. store-submission.json reports the actual state; a GitHub release does not imply Microsoft certification.

Chrome recommendations/handoff require the companion extension and the user's own signed-in YouTube session. Private videos, age/region restrictions, videos that disallow embedding and Windows S mode are not supported by this embedded player.

The GitHub EXE installer is not Authenticode-signed. Store MSIX packages require the exact existing Partner Center product identity and Microsoft certification.
