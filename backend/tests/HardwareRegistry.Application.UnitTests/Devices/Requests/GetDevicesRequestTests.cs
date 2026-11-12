using HardwareRegistry.Application.Common;
using HardwareRegistry.Application.Devices;
using HardwareRegistry.Application.Devices.Requests;
using HardwareRegistry.Application.UnitTests.TestUtilities;

namespace HardwareRegistry.Application.UnitTests.Devices.Requests
{
    public sealed class GetDevicesRequestTests
    {
        [Fact]
        public void Constructor_NoArguments_SortsByInventoryNumberAscendingOnFirstPage()
        {
            var request = new GetDevicesRequest();

            Assert.Equal(DeviceSortField.InventoryNumber, request.SortBy);
            Assert.Equal(SortOrder.Ascending, request.SortOrder);
            Assert.Equal(1, request.Page);
            Assert.Empty(ModelValidator.Validate(request));
        }

        [Fact]
        public void Validate_UndefinedSortField_ReportsSortBy()
        {
            var errors = ModelValidator.Validate(new GetDevicesRequest { SortBy = (DeviceSortField)42 });

            var error = Assert.Single(errors);
            Assert.Contains(nameof(GetDevicesRequest.SortBy), error.MemberNames);
        }

        [Fact]
        public void Validate_UndefinedSortOrder_ReportsSortOrder()
        {
            var errors = ModelValidator.Validate(new GetDevicesRequest { SortOrder = (SortOrder)42 });

            var error = Assert.Single(errors);
            Assert.Contains(nameof(GetDevicesRequest.SortOrder), error.MemberNames);
        }
    }
}
