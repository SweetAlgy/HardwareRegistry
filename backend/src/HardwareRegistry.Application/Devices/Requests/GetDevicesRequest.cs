using System.ComponentModel.DataAnnotations;
using HardwareRegistry.Application.Common;
using HardwareRegistry.Application.Common.Requests;

namespace HardwareRegistry.Application.Devices.Requests
{
    public sealed record GetDevicesRequest : SearchRequest
    {
        [EnumDataType(typeof(DeviceSortField))]
        public DeviceSortField SortBy { get; init; } = DeviceSortField.InventoryNumber;

        [EnumDataType(typeof(SortOrder))]
        public SortOrder SortOrder { get; init; } = SortOrder.Ascending;
    }
}
