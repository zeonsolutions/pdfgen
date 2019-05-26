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
        private string _outputRootPath;

        // Caminho do arquivo JSON
        private string _pathJson;

        // Nome do arquivo template
        private string _templateName = "";

        // Caminho do template
        private string _templatesRootPath;

        #endregion Atributos

        #region Construtor
        public PDFGenerator()
        {
            // Gera o GUID de identificação
            GuidGenerate();
        }

        #endregion Construtor

        #region Métodos Privados

        /// <summary>
        /// Gera o PDF. Necessário chamar o método Configure anteriormente.
        /// </summary>
        /// <param name="templateName">Nome do template a ser utilizado.</param>
        /// <param name="name">Nome do arquivo (PDF) que será gerado.</param>
        /// <param name="data">Dados do relatório. (JSON)</param>
        private async Task<string> BuildReport (string templateName, string name, string data)
        {
            string outputDirectoryPath = GetOutputDirectoryPathForTemplate(templateName);
            string renderPath = $"file:{Path.Combine(outputDirectoryPath, "template.html")}";
            string returnPath = Path.Combine(outputDirectoryPath, $"{name}.pdf");

            bool createdOutputDirectory = CreateTemplateOutputDirectory(templateName);
            bool hasValidTemplatePaths = ValidateTemplatePaths(templateName);

            if (createdOutputDirectory && hasValidTemplatePaths)
            {
                CopyTemplateFiles(templateName);
                CreateJsonFile(outputDirectoryPath, data);
            }
            else
            {
                throw new Exception("Error validating and creating paths.");
            }

            // Cria instância do browser background (utilizando Puppeteer)
            // para posterior renderização do relatório HTML
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new[]{ "--no-sandbox" }
            });

            // Cria uma nova página no browser interno
            var page = await browser.NewPageAsync();

            // Abre o relatório nessa nova página
            // Propriedade WaitUntilNavigation.Networkidle2 aguarda o carregamento dos
            // recursos do relatório (imagens, imports, etc).
            await page.GoToAsync(renderPath, WaitUntilNavigation.Networkidle2);

            // Gera o PDF
            await page.PdfAsync(returnPath, new PdfOptions {
                Format = PaperFormat.A4,
                PrintBackground = true
            });

            // Retorna o caminho do PDF
            return returnPath;
        }

        // Copiar arquivos de templates para a nova pasta (utilizando GUID)
        private bool CopyTemplateFiles (string templateName)
        {
            string templatePath = GetPathForTemplate(templateName);

            // Pega o caminho físico do diretório do template
            string directory = new FileInfo(templatePath).Directory.FullName;
            string outputDirectory = GetOutputDirectoryPathForTemplate(templateName);

            // Copia todos os arquivos desse diretório para a nova pasta de saída
            foreach(var file in Directory.GetFiles(directory))
                File.Copy(file, Path.Combine(outputDirectory, Path.GetFileName(file)));

            return true;
        }

        // Criar arquivo JSON com os dados do relatório
        private bool CreateJsonFile(string outputDirectoryPath, string data)
        {
            // Gera o arquivo .json com os dados do template
            string outputFilePath = Path.Combine(outputDirectoryPath, _jsonName);
            File.WriteAllText(outputFilePath, data);
            return true;
        }

        private bool CreateOutputRootDirectory(string path, bool force)
        {
            if (!force)
            {
                if (!Directory.Exists(path))
                {
                    throw new Exception("The output path does not exist. Enter a valid path OR change the value of the 'force' attribute to TRUE. (E.g. 'C:\\Project\\out\\')");
                }

                return false;
            }

            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                return true;
            }
            catch (Exception ex)
            {
                string m = ex.Message;
                m += "\n\n" + path;

                throw new Exception(m);
            }
        }

        private bool CreateTemplateOutputDirectory(string templateName)
        {
            try
            {
                if (!Directory.Exists(GetOutputDirectoryPathForTemplate(templateName)))
                {
                    return Directory.CreateDirectory(GetOutputDirectoryPathForTemplate(templateName)).Exists;
                }

                return true;
            }
            catch (Exception ex)
            {
                string m = ex.Message;
                m += "\n\n" + this._outputRootPath;

                throw new Exception(m);
            }
        }

        private string GetPathForTemplate(string templateName)
        {
            return Path.Combine(_templatesRootPath, templateName, "template.html");
        }

        private string GetOutputDirectoryPathForTemplate(string templateName)
        {
            return Path.Combine(_outputRootPath, templateName, _guidGeneration.ToString());
        }

        private void GuidGenerate()
        {
            // Gera o GUID de identificação
            this._guidGeneration = Guid.NewGuid();
        }

        private bool ValidateTemplatePaths(string templateName)
        {
            if (!File.Exists(GetPathForTemplate(templateName)))
            {
                throw new Exception("The template path does not exist. Enter a valid path. (E.g. 'C:\\Project\\templates\\foo\\template.html')");
            }

            string templateOutputDirectoryPath = GetOutputDirectoryPathForTemplate(templateName);

            if (!Directory.Exists(templateOutputDirectoryPath))
            {
                throw new Exception("The template output path does not exist.");
            }

            return true;
        }

        private bool ValidateRootPaths() {
            if (!Directory.Exists(_templatesRootPath))
            {
                throw new Exception("The templates root path does not exist. Enter a valid path. (E.g. 'C:\\Project\\templates')");
            }

            if (_force)
            {
                return true;
            }

            if (!Directory.Exists(_outputRootPath))
            {
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
        /// <param name="data">Dados do relatório. (JSON)</param>
        public async Task<string> Build (string templateName, string name, object data)
        {
            string dataSerie = "window.data = ";


            if (data.GetType() == typeof(string))
            {
                dataSerie += (data as string);
                return await this.BuildReport(templateName, name, dataSerie);
            }

            var jss = new JsonSerializerSettings() {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            // Serializa os dados recebidos em um Json (string)
            dataSerie += JsonConvert.SerializeObject(data, Formatting.None, jss);

            // Gera o PDF
            return await this.BuildReport(templateName, name, dataSerie);
        }

        /// <summary>
        /// Configura os caminhos que serão utilizados para geração de PDF
        /// </summary>
        /// <param name="templatesRootPath">Caminho relativo ou absoluto que contém a localização do diretório de templates (ex. 'C:\projeto\templates')</param>
        /// <param name="outputPath">Caminho relativo ou absoluto que contém a localização da pasta de saída (ex. 'C:\projeto\out\')</param>
        /// <param name="force">Se true, força a criação dos caminhos caso não existir</param>
        public bool Configure(string templatesRootPath, string outputRootPath, bool force = false)
        {
            // Caminho do diretório de templates
            this._templatesRootPath = Path.GetFullPath(templatesRootPath);

            // Caminho de saida raiz.
            this._outputRootPath = Path.GetFullPath(outputRootPath);

            // Forçar ou não criação do diretório de saída raíz
            this._force = force;

            if (!ValidateRootPaths())
            {
                return false;
            }

            return CreateOutputRootDirectory(outputRootPath, force);
        }

        #endregion Métodos Públicos
    }
}
