using System.Collections.Generic;

namespace TramChanh.People
{
    /// <summary>Supplies the things animals may fear: the player and motorbikes. Appends to <paramref name="into"/>.</summary>
    public interface IAnimalThreatSource
    {
        void Collect(List<AnimalThreat> into);
    }
}
