using NSubstitute;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Network;

namespace Toggl.Foundation.Tests.Helpers
{
    public static class ApiExceptions
    {
        private static IRequest request => Substitute.For<IRequest>();

        private static IResponse response => Substitute.For<IResponse>();

        public static object[] ClientExceptions
            => new[]
            {
                new object[] { new BadRequestException(request, response) },
                new object[] { new UnauthorizedException(request, response) },
                new object[] { new PaymentRequiredException(request, response) },
                new object[] { new ForbiddenException(request, response) },
                new object[] { new NotFoundException(request, response) },
                new object[] { new ApiDeprecatedException(request, response) },
                new object[] { new RequestEntityTooLargeException(request, response) },
                new object[] { new ClientDeprecatedException(request, response) },
                new object[] { new TooManyRequestsException(request, response) }
            };

        public static object[] ClientExceptionsWhichAreNotReThrownInSyncStates
            => new[]
            {
                new object[] { new BadRequestException(request, response) },
                new object[] { new PaymentRequiredException(request, response) },
                new object[] { new ForbiddenException(request, response) },
                new object[] { new NotFoundException(request, response) },
                new object[] { new RequestEntityTooLargeException(request, response) },
                new object[] { new TooManyRequestsException(request, response) }
            };

        public static object[] ServerExceptions
            => new[]
            {
                new object[] { new InternalServerErrorException(request, response) },
                new object[] { new BadGatewayException(request, response) },
                new object[] { new GatewayTimeoutException(request, response) },
                new object[] { new HttpVersionNotSupportedException(request, response) },
                new object[] { new ServiceUnavailableException(request, response) }
            };
    }
}
