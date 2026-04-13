using BTAutomation.Model;
using BTAutomation.Service;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace BTAutomation.Utils
{
    public class Arquivo
    {
        public static async Task ProcessarArquivo(string filePath, ILogger _logger, JakaService jaka)
        {
            string ultimaLinhaValida = "";
            //string headerCompleto = "Test Conditions, Measured Value, Lower Limit, Upper Limit, P/F, Sec, Code, Code Lsl, Code Usl, Meas Fine, Code Fine";
       
            // Lógica de leitura de trás para frente (mantida do seu código original)
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                long posicao = fs.Length - 1;
                while (posicao >= 0)
                {
                    fs.Seek(posicao, SeekOrigin.Begin);
                    if (fs.ReadByte() == '\n' || posicao == 0)
                    {
                        if (posicao != 0) fs.Seek(posicao + 1, SeekOrigin.Begin);
                        using (var sr = new StreamReader(fs, System.Text.Encoding.UTF8, false, 1024, leaveOpen: true))
                        {
                            string? linha = await sr.ReadLineAsync();
                            if (LinhaValida.EhLinhaValida(linha))
                            {
                                ultimaLinhaValida = linha;
                                break;
                            }
                        }
                    }
                    posicao--;
                }
            }
            // 1. Verificamos se a linha realmente contém o "RESULT"
            if (ultimaLinhaValida.Contains("RESULT"))
            {
                
                var partes = ultimaLinhaValida.Split(':');
                if (partes.Length > 1)
                {
                    string valorExtraido = partes[1].Trim();

                    _logger.LogInformation("Valor extraído do RESULT: {valor}", valorExtraido);
                    if(valorExtraido == "PASS")
                    {
                        await jaka.Send_PASS_BT1();
                    }
                    else
                    {
                        await jaka.Send_Fail_BT1();
                    }
                    //int statusClp = (valorExtraido == "PASS" || valorExtraido == "P") ? 1 : 0;
                    //_clpService.WriteToCLP(statusClp);
                }
            }

            // Verifica a ultima linha válida encontrada
            //if (string.IsNullOrEmpty(ultimaLinhaValida)) return;

            //var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            //{
            //    Delimiter = ":",
            //    TrimOptions = TrimOptions.Trim,
            //    MissingFieldFound = null
            //};

            //var textoFinal = headerCompleto + Environment.NewLine + ultimaLinhaValida;

            //using (var reader = new StringReader(textoFinal))
            //using (var csv = new CsvReader(reader, config))
            //{

            //    csv.Context.RegisterClassMap<TempTeste>();
            //    var registros = csv.GetRecords<DownloaderData>().ToList();
            //    _logger.LogInformation("Dados processados com sucesso do arquivo {file}. Status: {status}",
            //        Path.GetFileName(filePath), registros.FirstOrDefault()?.Teste);
            //     _clpService.WriteToCLP(registros.FirstOrDefault()?.Teste == "P" ? 1 : 0);

            //}
        }

        //public sealed class TempTeste : ClassMap<DownloaderData>
        //{
        //    public TempTeste() => Map(m => m.Teste).Name("P/F");
        //}
    }
}
