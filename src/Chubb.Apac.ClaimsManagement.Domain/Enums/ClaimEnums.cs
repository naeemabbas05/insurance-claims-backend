namespace Chubb.Apac.ClaimsManagement.Domain.Enums;

public enum ClaimType
{
    Motor,
    Property
}

public enum PolicyType
{
    Motor,
    Property
}

public enum ClaimStatus
{
    Submitted,
    UnderReview,
    AwaitingInformation,
    AssessmentInProgress,
    Approved,
    Declined,
    Settled,
    Closed
}

public enum ClaimantRole
{
    PolicyHolder,
    Insured,
    ThirdParty,
    Beneficiary,
    Representative
}

public enum PolicyStatus
{
    Active,
    Cancelled
}

public enum ClaimDocumentType
{
    ClaimForm,
    PoliceReport,
    Photograph,
    Invoice,
    MedicalReport,
    RepairEstimate,
    ProofOfOwnership,
    Other
}

public enum AssignmentRole
{
    ClaimsHandler,
    Adjuster,
    Assessor,
    Approver
}

public enum StaffRole
{
    ClaimsHandler,
    Adjuster,
    Assessor,
    Approver,
    FinanceOfficer,
    Administrator
}

public enum InformationRequestStatus
{
    Open,
    Responded,
    Cancelled
}

public enum SettlementStatus
{
    Proposed,
    Approved,
    Paid,
    Cancelled
}
