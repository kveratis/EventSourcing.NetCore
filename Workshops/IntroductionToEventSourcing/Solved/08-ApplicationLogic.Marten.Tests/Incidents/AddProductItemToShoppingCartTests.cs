using System.Net;
using ApplicationLogic.Marten.Immutable.ShoppingCarts;
using Bogus;
using Ogooreck.API;
using Xunit;
using static Ogooreck.API.ApiSpecification;
using static ApplicationLogic.Marten.Tests.Incidents.Scenarios;
using static ApplicationLogic.Marten.Tests.Incidents.Fixtures;

namespace ApplicationLogic.Marten.Tests.Incidents;

public class AddProductItemToShoppingCartTests(ApiSpecification<Program> api):
    IClassFixture<ApiSpecification<Program>>
{
    [Theory]
    [InlineData("immutable")]
    [InlineData("mutable")]
    [InlineData("mixed")]
    public Task CantAddProductItemToNotExistingShoppingCart(string apiPrefix) =>
        api.Given()
            .When(
                POST,
                URI(ShoppingCartProductItems(apiPrefix, ClientId, NotExistingShoppingCartId)),
                BODY(new AddProductRequest(ProductItem))
            )
            .Then(NOT_FOUND);

    [Theory]
    [InlineData("immutable")]
    [InlineData("mutable")]
    [InlineData("mixed")]
    public Task AddsProductItemToEmptyShoppingCart(string apiPrefix) =>
        api.Given(OpenedShoppingCart(apiPrefix, ClientId))
            .When(
                POST,
                URI(ctx => ShoppingCartProductItems(apiPrefix, ClientId, ctx.GetCreatedId<Guid>())),
                BODY(new AddProductRequest(ProductItem))
            )
            .Then(NO_CONTENT);

    [Theory]
    [InlineData("immutable")]
    [InlineData("mutable")]
    [InlineData("mixed")]
    public Task AddsProductItemToNonEmptyShoppingCart(string apiPrefix) =>
        api.Given(
                OpenedShoppingCart(apiPrefix, ClientId),
                WithProductItem(apiPrefix, ClientId, ProductItem)
            )
            .When(
                POST,
                URI(ctx => ShoppingCartProductItems(apiPrefix, ClientId, ctx.GetCreatedId<Guid>())),
                BODY(new AddProductRequest(ProductItem))
            )
            .Then(NO_CONTENT);

    [Theory]
    [InlineData("immutable")]
    [InlineData("mutable")]
    [InlineData("mixed")]
    public Task CantAddProductItemToConfirmedShoppingCart(string apiPrefix) =>
        api.Given(
                OpenedShoppingCart(apiPrefix, ClientId),
                WithProductItem(apiPrefix, ClientId, ProductItem),
                ThenConfirmed(apiPrefix, ClientId)
            )
            .When(
                POST,
                URI(ctx => ShoppingCartProductItems(apiPrefix, ClientId, ctx.GetCreatedId<Guid>())),
                BODY(new AddProductRequest(ProductItem))
            )
            .Then(CONFLICT);

    [Theory]
    [InlineData("immutable")]
    [InlineData("mutable")]
    [InlineData("mixed")]
    public Task CantAddProductItemToCanceledShoppingCart(string apiPrefix) =>
        api.Given(
                OpenedShoppingCart(apiPrefix, ClientId),
                WithProductItem(apiPrefix, ClientId, ProductItem),
                ThenCanceled(apiPrefix, ClientId)
            )
            .When(
                POST,
                URI(ctx => ShoppingCartProductItems(apiPrefix, ClientId, ctx.GetCreatedId<Guid>())),
                BODY(new AddProductRequest(ProductItem))
            )
            .Then(CONFLICT);

    [Theory]
    [InlineData("immutable")]
    [InlineData("mutable")]
    [InlineData("mixed")]
    public Task ReturnsNonEmptyShoppingCart(string apiPrefix) =>
        api.Given(
                OpenedShoppingCart(apiPrefix, ClientId),
                WithProductItem(apiPrefix, ClientId, ProductItem)
            )
            .When(GET, URI(ctx => ShoppingCart(apiPrefix, ClientId, ctx.GetCreatedId<Guid>())))
            .Then(OK);

    private static readonly Faker Faker = new();
    private readonly Guid NotExistingShoppingCartId = Guid.NewGuid();
    private readonly Guid ClientId = Guid.NewGuid();
    private readonly ProductItemRequest ProductItem = new(Guid.NewGuid(), Faker.Random.Number(1, 500));
}
