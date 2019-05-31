using System;
using System.IO;
using System.Threading.Tasks;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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


        private string AddPageNumber(string outputDirectoryPath, string auxFilePath, string name)
        {
            string returnPath = Path.Combine(outputDirectoryPath, $"{name}.pdf");

            byte[] bytes = File.ReadAllBytes(auxFilePath);
            Font blackFont = FontFactory.GetFont("Arial", 12, Font.NORMAL, BaseColor.BLACK);
            using (MemoryStream stream = new MemoryStream())
            {
                PdfReader reader = new PdfReader(bytes);
                using (PdfStamper stamper = new PdfStamper(reader, stream))
                {
                    int pages = reader.NumberOfPages;
                    for (int i = 1; i <= pages; i++)
                    {
                        ColumnText.ShowTextAligned(stamper.GetUnderContent(i), Element.ALIGN_RIGHT, new Phrase(i.ToString(), blackFont), 568f, 15f, 0);
                    }
                }
                bytes = stream.ToArray();
            }

            
            File.WriteAllBytes(returnPath, bytes);
            bytes = null;           

            if (File.Exists(auxFilePath))
                File.Delete(auxFilePath);

            return returnPath;
        }

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
            string auxFilePath = Path.Combine(outputDirectoryPath, $"{name}_aux.pdf");

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
                Args = new[]{ 
                    "--no-sandbox",
                    "--disable-web-security", 
                    "--force-empty-corb-allowlist", 
                    "--enable-features=NetworkService",
                    "--allow-external-pages",
                    "--allow-file-access-from-files" 
                    }
            });

            // Cria uma nova página no browser interno
            var page = await browser.NewPageAsync();
            await page.SetBypassCSPAsync(true);

            // Abre o relatório nessa nova página
            // Propriedade WaitUntilNavigation.Networkidle2 aguarda o carregamento dos
            // recursos do relatório (imagens, imports, etc).
            var r = await page.GoToAsync(renderPath, WaitUntilNavigation.Networkidle2);
             
            // Gera o PDF
            await page.PdfAsync(auxFilePath, new PdfOptions {
                Format = PaperFormat.A4,
                PrintBackground = true,
                MarginOptions = new MarginOptions() { Left = "30px", Right = "30px", Top = "40px", Bottom = "40px" }
            });

            // Adiciona Números na Página (utilizando itextSharp)
            string returnPath = AddPageNumber(outputDirectoryPath, auxFilePath, name);
        
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

            // ALTERAR AQUI
            //File.WriteAllText(outputFilePath, data);
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
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
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
