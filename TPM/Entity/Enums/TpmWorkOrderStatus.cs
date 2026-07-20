namespace FSM.Entity.Enums
{
    public enum TpmWorkOrderStatus
    {
        New,
        Acknowledged,
        PendingAuthorization,
        Scheduled,
        InProgress,
        AwaitingParts,
        PendingInfo,
        Approved,
        Denied,
        InvoiceSubmitted,
        PaymentPending,
        Reconciled,
        Closed,
        Escalated
    }
}
