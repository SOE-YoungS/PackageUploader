﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Config;
using Json.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.Application.Operations
{
    internal class ValidateConfigOperation : Operation
    {
        private readonly ValidateConfigOperationConfig _config;
        private readonly ValidationOptions _validationOptions;

        public ValidateConfigOperation(IOptions<ValidateConfigOperationConfig> config, ILogger<GenerateConfigTemplateOperation> logger) : base(logger)
        {
            _config = config.Value; 
            _validationOptions = new ValidationOptions
            {
                Log = new JsonSchemaLogger(_logger),
                OutputFormat = OutputFormat.Basic,
            };
        }

        protected async override Task ProcessAsync(CancellationToken ct)
        {
            _logger.LogDebug("Validating config file.");

            var configFile = new FileInfo(_config.ConfigFilepath);
            if (!configFile.Exists)
            {
                throw new FileNotFoundException("Config file does not exist.");
            }

            JsonDocument configJson = null;
            try
            {
                using var configFileStream = configFile.OpenRead();
                configJson = await JsonDocument.ParseAsync(configFileStream, cancellationToken: ct).ConfigureAwait(false);
            }
            catch (JsonException e)
            {
                throw new JsonException($"Config file Json parsing error: {e.Message}.", e);
            }

            var schema = await GetJsonSchema().ConfigureAwait(false);
            var validationResult = schema.Validate(configJson.RootElement, _validationOptions);
            if (validationResult.IsValid)
            {
                _logger.LogInformation("Valid config file.");
            }
            else
            {
                throw new Exception($"Validation error: '{validationResult.Message}' in '{validationResult.SchemaLocation}'");
            }
        }

        private static async Task<JsonSchema> GetJsonSchema()
        {
            var assembly = Assembly.GetExecutingAssembly();
            const string resourceName = "GameStoreBroker.Application.Schemas.PackageUploaderOperationConfigSchema.json";
            using var schemaStream = assembly.GetManifestResourceStream(resourceName);
            if (schemaStream is null)
            {
                throw new Exception($"Error while accessing the schema.");
            }
            else
            {
                var schema = await JsonSchema.FromStream(schemaStream).ConfigureAwait(false);
                return schema;
            }
        }

        private class JsonSchemaLogger : ILog
        {
            private readonly ILogger _logger;

            public JsonSchemaLogger(ILogger logger)
            {
                _logger = logger;
            }
            public void Write(Func<string> message, int indent = 0)
            {
                var logmessage = message();
                _logger.LogTrace(logmessage);
            }
        }
    }
}
