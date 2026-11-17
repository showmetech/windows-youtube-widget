# 1.8.4

- Remove metadados de depuração dos binários de produção e bloqueia pacotes que contenham caminhos de perfil nos executáveis e DLLs do aplicativo.
- Mantém o bundle de x86, x64 e ARM64, isolamento por usuário e sessão, startup preservado e proteção contra dados locais na distribuição.

# 1.8.3

- Consolida a distribuição para Microsoft Store em um bundle de x86, x64 e ARM64, com identidade oficial e versão única para o conteúdo final.
- Inclui o isolamento por usuário e sessão, a proteção contra dados privados nos pacotes e a preservação da escolha de startup da versão 1.8.2.

# 1.8.2

- Associa os pacotes MSIX à identidade oficial Showmetech.YouTubeWidget, reservada no Microsoft Partner Center.
- Isola a conexão local de recomendações por usuário e sessão do Windows, evitando conflitos em computadores compartilhados.
- Bloqueia pacotes que contenham perfis de navegador, preferências locais, logs, arquivos de teste ou marcadores de credenciais.
- Mantém a escolha de inicialização automática do usuário durante atualizações; novas instalações começam com startup ativado.
- A publicação inicial na Microsoft Store permanece sujeita à submissão e certificação; preserva as funções da versão 1.8.1.

# 1.8.1

- Recolhe o widget em clique fora quando o pin estiver desmarcado, incluindo cliques na área de trabalho que não alteram o foco.
- Cliques dentro do vídeo, nas configurações e no menu continuam disponíveis; operações de carregamento não impedem o recolhimento por clique externo.

# 1.8.0

- Aplica a linguagem Fluent 2 ao tema WPF: superfícies, tipografia, cartões, controles e estados de interação.
- Melhora foco de teclado e contraste do texto sobre a cor de destaque.
- Preserva player compacto, temas claro/escuro/sistema, opacidade padrão de 100% e configurações sem rolagem.

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






