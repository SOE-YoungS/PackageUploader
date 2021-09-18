﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Config;
using GameStoreBroker.Application.Extensions;
using GameStoreBroker.ClientApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.Application.Operations
{
    internal class ImportPackagesOperation : Operation
    {
        private readonly IGameStoreBrokerService _storeBrokerService;
        private readonly ILogger<ImportPackagesOperation> _logger;
        private readonly ImportPackagesOperationConfig _config;

        public ImportPackagesOperation(IGameStoreBrokerService storeBrokerService, ILogger<ImportPackagesOperation> logger, IOptions<ImportPackagesOperationConfig> config) : base(logger)
        {
            _storeBrokerService = storeBrokerService ?? throw new ArgumentNullException(nameof(storeBrokerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        }

        protected override async Task ProcessAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting {operationName} operation.", _config.GetOperationName());

            var product = await _storeBrokerService.GetProductAsync(_config, ct).ConfigureAwait(false);
            var originPackageBranch = await _storeBrokerService.GetGamePackageBranch(product, _config, ct).ConfigureAwait(false);
            var destinationPackageBranch = await _storeBrokerService.GetDestinationGamePackageBranch(product, _config, ct).ConfigureAwait(false);

            await _storeBrokerService.ImportPackagesAsync(product, originPackageBranch, destinationPackageBranch, _config.MarketGroupId, _config.Overwrite, ct).ConfigureAwait(false);

            if (_config.AvailabilityDateConfig is not null)
            {
                await _storeBrokerService.SetXvcAvailabilityDateAsync(product, destinationPackageBranch, _config.MarketGroupId, _config.AvailabilityDateConfig.AvailabilityDate, ct).ConfigureAwait(false);
                _logger.LogInformation("Availability date for Xvc packages set");

                await _storeBrokerService.SetUwpAvailabilityDateAsync(product, destinationPackageBranch, _config.MarketGroupId, _config.AvailabilityDateConfig.AvailabilityDate, ct).ConfigureAwait(false);
                _logger.LogInformation("Availability date for Uwp packages set");
            }

            if (_config.MandatoryDateConfig is not null)
            {
                await _storeBrokerService.SetUwpMandatoryDateAsync(product, destinationPackageBranch, _config.MarketGroupId, _config.MandatoryDateConfig.MandatoryDate, ct).ConfigureAwait(false);
                _logger.LogInformation("Mandatory date for Uwp packages set");
            }
        }
    }
}