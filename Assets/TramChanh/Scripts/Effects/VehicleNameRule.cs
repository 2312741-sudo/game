using System;

namespace TramChanh.Effects
{
    /// <summary>
    /// Which people-layer objects are vehicles. PeopleController names the bodies it creates "Motorbike_&lt;id&gt;" (ambient bikes)
    /// and "VehicleCustomer" (the takeaway customer). Reading the names lets the effects module find them without
    /// changing the people code.
    /// </summary>
    public static class VehicleNameRule
    {
        public const string AmbientBikePrefix = "Motorbike_";
        public const string CustomerVehicleName = "VehicleCustomer";

        public static bool IsVehicle(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                return false;
            }
            return objectName.StartsWith(AmbientBikePrefix, StringComparison.Ordinal)
                || objectName.StartsWith(CustomerVehicleName, StringComparison.Ordinal);
        }
    }
}
