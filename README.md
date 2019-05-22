## PDFGenerator

**PDFGenerator**  - Biblioteca que realiza a exportação de uma página HTML (Hosted) para PDF.


### O que é?

PDFGenerator foi criada originalmente para a exportação de páginas HTML + CSS + JS que utilizam uma fonte de dados externa, em arquivos de formato PDF.

É possível exportar dados em formato JSON ou outros tipos de coleção diretamente (ex. List\<Cliente\>) para consultas e relatórios do seu sistema.

As funcionalidades da biblioteca foram construídas utilizando o [Puppeter](http://www.puppeteersharp.com/api/index.html) e também o [Newtonsoft](https://www.newtonsoft.com/json).

### Características

+ Geração de PDFs que utilizam fonte de dados externa.
+ Suporte HTML + CSS.
+ Geração rápida utilizando kernel do Chromium.


### Observações Importantes (antes de utilizar)

+ Necessário o Chromium (google Chrome ou kernel do Chromium).
+ Permissão nas pastas para leitura e gravação.
+ Para fontes de dados externa, template deve basear-se em um arquivo chamado *data.js*. Dúvidas, basear-se no template da pasta ***exemplos***.


### Utilização

A utilização é bastante simples. Você necessitará de um template HTML que pode ficar no diretório de sua aplicação. Um exemplo do HTML pode ser visto na pasta ***exemplos***.

Em seguida basta realizar a chamada de dois métodos. O primeiro é o método **Configure**:

```csharp
PDFGenerator pdfGenerator = new PDFGenerator();
pdfGenerator.Configure(templatePath, outputPath, force);
```

+ **templatePath** - Caminho que contém a localização do template (arquivo HTML). Pode ser um caminho absoluto ou relativo. Parâmetro tipo ***string***.
+ **outputPath** - Caminho de saída do arquivo.  Parâmetro tipo ***string***.
+ **Force** - Forçar a criação do caminho de saída caso não existir. Parâmetro tipo ***bool***.

<br/>
Após configurar o gerador, é possível chamar a função de geração.

```csharp
string p =  await pdfGenerator.Build(filePDFName, data);
```

Onde:

+ **teste.pdf** - o nome do arquivo de saída (ex. *"teste.pdf"*). Parâmetro tipo ***string***.
+ **data** - coleção de dados. Pode ser um Json já serializado (formato *string*) ou uma coleção do seu sistema (ex. List\<Produto\>). Parâmetro tipo ***object***.
<br>

### Exemplo de Utilização

Segue um exemplo explicado de utilização.

Primeiramente definiremos os caminhos do template e de saída. Também iremos definir o valor do parâmetro *force*, que nesse caso irá forçar a criação do caminho output.
```csharp
string templatePath =  @"./templates/relatorio1/template.html";
string outputPath =  @"./out/";
bool force =  true;
```
Repare que os caminhos utilizados são do tipo *relativo*, porém é possível utilizar também caminhos absolutos (ex. *C:\SeuSistema\templates\relatorio1\template.html").

Em seguida vamos configurar e executar o gerador:

```csharp
// Cria instância do Gerador
PDFGenerator.PDFGenerator pdfGenerator =  new  PDFGenerator.PDFGenerator();
// Configura o gerador com os caminhos
bool retorno =  pdfGenerator.Configure(templatePath, outputPath, force);
// Chama a geração do arquivo PDF
string pathFile =  await pdfGenerator.Build("teste.pdf", GetData());

return  ret;
```

Um exemplo do método GetData() pode ser conferido abaixo:

```csharp
public List<Cliente> GetData() {
	
	List<Cliente> clientes =  new  List<Cliente>();

	clientes.Add(new Cliente { id = 1, name = "Joao", profissao = "Pedreiro", dataNasc = "31/05/1980"});
	clientes.Add(new Cliente { id = 2, name = "Pedro", profissao = "Arquiteto", dataNasc = "04/04/1988"});

	return clientes;
}
```