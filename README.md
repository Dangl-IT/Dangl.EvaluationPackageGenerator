# Dangl.EvaluationPackageGenerator

This app is used to automate the generation of evaluation packages for the [GAEB & AVA .Net Libraries](https://www.dangl-it.com/products/gaeb-ava-net-library/) and
the [AVACloud GAEB SaaS Webservice](https://www.dangl-it.com/products/avacloud-gaeb-saas/). Customers may use this tool to manually download packages.

The CLI tool is available for download in the assets section:  
https://docs.dangl-it.com/ProjectAssets/Dangl.EvaluationPackageGenerator/latest

## CLI Usage

You can use the converter from the command line, it is available in the zip package as `Dangl.EvaluationPackageGenerator.exe`.

    Dangl.EvaluationPackageGenerator.exe --mygetapikey <MyApiKey>

The following CLI parameters are available:

| Parameter | Description |
|-----------|-------------|
| --mygetusername | Optional, defaults to `georgdangl`. The name of the MyGet account. This must match the account of the API Key |
| --mygetapikey        | Required, the API Key to access the MyGet account. The account must have access to the private `dangl-ava` feed |
| --outputpath        | Optional, defaults to `./`. Where the generated package should be placed |
| --readmepath | Optional. If present, this file will be copied into the generated package |
| --includeprerelease | Optional. Either `true` or `false`, whether to include beta packages or only stable versions|
| --help    | Display options |
