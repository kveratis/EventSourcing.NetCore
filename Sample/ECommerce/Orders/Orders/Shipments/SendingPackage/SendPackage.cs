using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Core.Commands;
using Core.Requests;
using MediatR;
using Orders.Products;

namespace Orders.Shipments.SendingPackage
{
    public class SendPackage : ICommand
    {
        public Guid OrderId { get; }

        public IReadOnlyList<ProductItem> ProductItems { get; }

        private SendPackage(
            Guid orderId,
            IReadOnlyList<ProductItem> productItems
        )
        {
            OrderId = orderId;
            ProductItems = productItems;
        }

        public static SendPackage Create(
            Guid orderId,
            IReadOnlyList<ProductItem> productItems
        )
        {
            Guard.Against.Default(orderId, nameof(orderId));
            Guard.Against.NullOrEmpty(productItems, nameof(productItems));

            return new SendPackage(orderId, productItems);
        }
    }

    public class HandleSendPackage:
        ICommandHandler<SendPackage>
    {
        private readonly ExternalServicesConfig externalServicesConfig;
        private readonly IExternalCommandBus externalCommandBus;

        public HandleSendPackage(ExternalServicesConfig externalServicesConfig, IExternalCommandBus externalCommandBus)
        {
            this.externalServicesConfig = externalServicesConfig;
            this.externalCommandBus = externalCommandBus;
        }

        public async Task<Unit> Handle(SendPackage command, CancellationToken cancellationToken)
        {
            await externalCommandBus.Post(
                externalServicesConfig.ShipmentsUrl!,
                "shipments",
                command,
                cancellationToken);

            return Unit.Value;
        }
    }
}
