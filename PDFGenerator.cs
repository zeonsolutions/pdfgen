using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace PDFGenerator
{
    public class PDFGenerator
    {
        #region Atributos

        // Forçar criação do diretório de saída (caso não existir)
        private bool _force;

        //  Guid gerada pela dll (para controle)
        private Guid _guidGeneration;

        // Nome do arquivo JSON
        private string _jsonName = "data.js";

        // Caminho do diretório de saída (output)
        private string _outputPath;

        // Caminho do arquivo JSON
        private string _pathJson;

        // Nome do arquivo template
        private string _templateName = "";

        // Caminho do template
        private string _templatePath;

        #endregion Atributos


        #region Construtor
        public PDFGenerator() {
            // Gera o GUID de identificação
            GuidGenerate();
        }

        #endregion Construtor


        #region Métodos Privados

        /// <summary>
        /// Gera o PDF. Necessário chamar o método Configure anteriormente.
        /// </summary>
        /// <param name="name">Nome do arquivo (pdf) que será gerado. </param>
        /// <param name="data">Dados do relatório. Aceita string (lista em formato JSON) ou uma List<T></param>
        private async Task<string> BuildReport (string name, string data) {
            string pathRetorno = string.Empty;

            // Realiza a verificação se os paths são válidos e
            // cria o diretório de saída caso não existir (apenas se o atributo force = TRUE)
            // ATENÇÃO DESENVOLVEDOR: deixar com apenas um & para passar pelos dois métodos
            if (ValidatePaths() & CreateDirectoryPaths()) {
                // Copiar arquivos de template e gerar o JSON com os dados
                CopyTemplateFiles();
                CreateJsonFile(data);
            }
            else {
                throw new Exception("Error validating and creating paths.");
            }
            
            // Cria instância do browser background (utilizando Puppeteer) 
            // para posterior renderização do relatório HTML
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true
            });

            // Cria uma nova página no browser interno
            var page = await browser.NewPageAsync();

            // Abre o relatório nessa nova página
            // Propriedade WaitUntilNavigation.Networkidle2 aguarda o carregamento dos
            // recursos do relatório (imagens, imports, etc).
            await page.GoToAsync(this._outputPath + this._templateName, WaitUntilNavigation.Networkidle2);

            // Caminho do PDF
            pathRetorno = this._outputPath + name;

            // Gera o PDF
            await page.PdfAsync(this._outputPath + name, new PdfOptions {
                Format = PaperFormat.A4,
                PrintBackground = true
            });     

            // Retorna o caminho do PDF
            return pathRetorno;
        }

        // Copiar arquivos de templates para a nova pasta (utilizando GUID)
        private bool CopyTemplateFiles () {

            // Pega o caminho físico do diretório do template
            string directory = new FileInfo(this._templatePath).Directory.FullName;

            // Copia todos os arquivos desse diretório para a nova pasta de saída
            foreach(var file in Directory.GetFiles(directory))
                File.Copy(file, Path.Combine(this._outputPath, Path.GetFileName(file)));

            return true;
        }

        // Criar arquivo JSON com os dados do relatório
        private bool CreateJsonFile(string data) {
            // Gera o arquivo .json com os dados do template
            File.WriteAllText(this._pathJson, data);
            return true;
        }

        private bool CreateDirectoryPaths() {

            // Cria-se os diretórios apenas se a varíavel for true
            if (!this._force)
                return false;

            try {
                // Criação do diretório de saída, caso não existir
                if (!Directory.Exists(this._outputPath)) {
                    Directory.CreateDirectory(this._outputPath);
                }
            }
            catch (Exception ex) {
                string m = ex.Message;
                m += "\n\n" + this._templatePath;
                m += "\n\n" + this._outputPath;

                throw new Exception(m);
            }

            return true;
        }

        private void GetTemplateName() {
            //Pega o nome do arquivo template (apenas o nome)
            this._templateName = new FileInfo(this._templatePath).Name;
        }

        private void GuidGenerate() {
            // Gera o GUID de identificação
            this._guidGeneration = Guid.NewGuid();
        }

        private bool ValidatePaths() {

            // Se o diretório não existir e a variável force estiver com o valor false
            // Informa uma Exception dizendo que o caminho não existe.
            if (!File.Exists(this._templatePath)) {
                throw new Exception("The template path does not exist. Enter a valid path. (E.g. 'C:\\Project\\template.html')");
            }
            
            // Pega o nome do arquivo template
            GetTemplateName();

            // Se o diretório não existir e a variável force estiver com o valor false
            // Informa uma Exception dizendo que o caminho não existe.
            if (!Directory.Exists(this._outputPath) && !this._force) {
                throw new Exception("The output path does not exist. Enter a valid path OR change the value of the 'force' attribute to TRUE. (E.g. 'C:\\Project\\out\\')");
            }

            return true;
        }

        #endregion Métodos Privados


        #region Métodos Públicos

        /// <summary>
        /// Gera o PDF. Obrigatório chamar o método Configure antes de usar essa função.
        /// </summary>
        /// <param name="name">Nome do arquivo (pdf) que será gerado. </param>
        /// <param name="data">Dados do relatório. Aceita string (lista em formato JSON) ou uma List<T></param>
        public async Task<string> Build (string name, object data) {

            string dataSerie = string.Empty;

            if (data.GetType() == typeof(string)) {
                dataSerie = "export default " + (data as string);
                return await this.BuildReport(name, dataSerie);     
            }

            // Serializa os dados recebidos em um Json (string)
            dataSerie = JsonConvert.SerializeObject(data, Formatting.None);

            dataSerie = "export default " + dataSerie;

            // Gera o PDF
            return await this.BuildReport(name, dataSerie);
        }

        /// <summary>
        /// Configura os caminhos que serão utilizados para geração de PDF
        /// </summary>
        /// <param name="templatePath">Caminho lógico ou absoluto que contém a localização do template (ex. 'C:\projeto\template.html')</param>
        /// <param name="outputPath">Caminho lógico ou absoluto que contém a localização da pasta de saída (ex. 'C:\projeto\out\')</param>
        /// <param name="force">Se true, força a criação dos caminhos caso não existir</param>
        public bool Configure(string templatePath, string outputPath, bool force = false) {

            // Caminho do template
            this._templatePath = Path.GetFullPath(templatePath);
            // Novo caminho de saída, utilizando o GUID gerado para a requisição
            this._outputPath += string.Format(@"{0}{1}\", Path.GetFullPath(outputPath), this._guidGeneration.ToString());
            // Caminho completo do JSON de saída (utilizado no relatório)
            this._pathJson = string.Format(@"{0}{1}", this._outputPath, this._jsonName);
            // Forçar ou não criação dos diretórios de saída
            this._force = force;

            // Validar caminhos criados
            return ValidatePaths();
        }
        #endregion Métodos Públicos
    }


}