## BTAutomation

É um service que executa em backgroud que busca os dados do resultado do teste BT no diretório `C:\DGS\LOGS`. Esse serviço é um fileWatch que dispara quando um arquivo é alterado.

## Configuração  

No arquivo `FileWatcherService.cs` é configurado o path do diretório de teste.  
No arquivo `JakaService.cs` em Service é feita a configuração do IP do jaka e da porta.  

## Execução em produção  
Para executar como serviço windows é necessario usar o comando como admin no cmd:  
```
sc create NomeServiço binPath= "C:\meu_caminho"
```