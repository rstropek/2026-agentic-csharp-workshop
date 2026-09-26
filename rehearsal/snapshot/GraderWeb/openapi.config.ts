import { GeneratorConfig } from 'ng-openapi';

// Generates the Angular API client from the OpenAPI spec that the .NET build writes
// (see ../tech-docs/api-client.md). The output is generated code and not committed.
const config: GeneratorConfig = {
  input: '../GraderApi/openapi/GraderApi.json',
  output: './src/app/api',
  options: {
    dateType: 'string',
    enumStyle: 'union',
  },
};

export default config;
