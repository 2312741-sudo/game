namespace TramChanh.Customers
{
    /// <summary>Per-seat customer phase reported by <see cref="CustomerDirector.PhaseAt"/>.</summary>
    public enum CustomerPhase
    {
        /// <summary>No customer: the seat can be assigned (if its point has no live order).</summary>
        Free,
        /// <summary>Walking mode: seat reserved, customer walking to it, no order yet.</summary>
        Arriving,
        /// <summary>Order requested and live (legacy mode: from spawn until the table is freed).</summary>
        Seated,
        /// <summary>Walking mode: the order ended, the customer eats for <see cref="CustomerDirectorSettings.EatSeconds"/>.</summary>
        Eating,
        /// <summary>Walking mode: the customer stood up and walks away; the seat is still reserved.</summary>
        Leaving
    }
}
