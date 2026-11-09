# cesi — pacote Windows x64 para teste estrutural

Extraia todo o ZIP e abra `cesi.exe`. Mantenha `Syntax.dll` ao lado do executável. O runtime .NET é incluído; esta publicação é autocontida. A DLL deste pacote é compilada dos fontes para este teste, sem reutilizar o binário original do repositório.

## O que testar

1. Abra o Roblox `version-bc1bf755618240e8`, entre em uma experiência e espere o personagem carregar.
2. Abra cesi e use Conectar, ou habilite auto inject nas configurações.
3. Confira o estado **Conectado · teste estrutural**, o nome da conta e a foto no cabeçalho e na aba de clientes. O botão de execução permanece desativado neste build.
4. Teste troca de conta, fechamento/reabertura do processo, stream mode, temas e fontes. Registre a mensagem exata se a descoberta falhar.

O perfil exige a versão e o SHA-256 do executável observados no relatório. Não reutiliza endereços absolutos de uma sessão. A seleção do DataModel compara cadeias independentes e valida Workspace, Players e LocalPlayer. O nome Ugc não é usado como único critério.

## Limite do teste

Este pacote testa descoberta do DataModel, leitura da identidade e interface/avatar. Não injeta nem executa scripts. Os campos de bytecode e capabilities continuam candidatos no relatório, e a validação de execução ainda está pendente. O core informa esse estado explicitamente, em vez de anunciar injeção concluída.

O core de teste usa OpenProcess/ReadProcessMemory para as observações confirmadas e abre o processo apenas para consulta/leitura. A compilação não comprova funcionamento no processo local do usuário; esse é o teste a realizar no Windows.

Veja `build-manifest.json` para hashes e tamanho dos binários.
