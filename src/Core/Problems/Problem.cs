﻿using Altinn.Authorization.ProblemDetails;
using System.Net;

namespace Altinn.Authentication.Core.Problems;
/// <summary>
/// Problem descriptors for the Authentication UI BFF.
/// </summary>
public static class Problem
{
    private static readonly ProblemDescriptorFactory _factory
        = ProblemDescriptorFactory.New("AUTH");

    /// <summary>
    /// Gets a <see cref="ProblemDescriptor"/>.
    /// </summary>
    public static ProblemDescriptor Reportee_Orgno_NotFound{ get; }
        = _factory.Create(0, HttpStatusCode.BadRequest, "Can't resolve the Organisation Number from the logged in Reportee PartyId.");

    /// <summary>
    /// Gets a <see cref="ProblemDescriptor"/>.
    /// </summary>
    public static ProblemDescriptor Rights_NotFound_Or_NotDelegable { get; }
        = _factory.Create(1, HttpStatusCode.BadRequest, "One or more Right not found or not delegable.");

    /// <summary>
    /// Gets a <see cref="ProblemDescriptor"/>.
    /// </summary>
    public static ProblemDescriptor Rights_FailedToDelegate { get; }
        = _factory.Create(2, HttpStatusCode.BadRequest, "The Delegation failed.");


    /// <summary>
    /// Gets a <see cref="ProblemDescriptor"/>.
    /// </summary>
    public static ProblemDescriptor SystemUser_FailedToCreate { get; }
        = _factory.Create(3, HttpStatusCode.BadRequest, "Failed to create the SystemUser.");

    /// <summary>
    /// Gets a <see cref="ProblemDescriptor"/>.
    /// </summary>
    public static ProblemDescriptor SystemUser_AlreadyExists { get; }
        = _factory.Create(4, HttpStatusCode.BadRequest, "Failed to create new SystemUser, existing SystemUser tied to the given System-Id.");

    /// <summary>
    /// Gets a <see cref="ProblemDescriptor"/>.
    /// </summary>
    public static ProblemDescriptor Generic_EndOfMethod { get; }
        = _factory.Create(5, HttpStatusCode.BadRequest, "Default error at the end of logic chain. Not supposed to appear.");

    /// <summary>
    /// Gets a <see cref="ProblemDescriptor"/>.
    /// </summary>
    public static ProblemDescriptor ExternalRequestIdAlreadyAccepted { get; }
        = _factory.Create(6, HttpStatusCode.BadRequest, "The combination of External Ids refer to an already Accepted SystemUser.");

    /// <summary>
    /// Gets a <see cref="ProblemDescriptor"/>.
    /// </summary>
    public static ProblemDescriptor ExternalRequestIdPending { get; }
        = _factory.Create(7, HttpStatusCode.BadRequest, "The combination of External Ids refer to a Pending Request, please reuse or delete.");

    /// <summary>
    /// Gets a <see cref="ProblemDescriptor"/>.
    /// </summary>
    public static ProblemDescriptor ExternalRequestIdDenied { get; }
        = _factory.Create(8, HttpStatusCode.BadRequest, "The combination of External Ids refer to a Denied Request, please delete and renew the Request.");

    /// <summary>
    /// Gets a <see cref="ProblemDescriptor"/>.
    /// </summary>
    public static ProblemDescriptor ExternalRequestIdRejected { get; }
        = _factory.Create(9, HttpStatusCode.BadRequest, "The combination of External Ids refer to a Rejected Request, please delete and renew the Request.");

    /// <summary>
    /// Gets a <see cref="ProblemDescriptor"/>.
    /// </summary>
    public static ProblemDescriptor RequestNotFound { get; }
        = _factory.Create(10, HttpStatusCode.NotFound, "The Id does not refer to a Request in our system.");

    /// <summary>
    /// Gets a <see cref="ProblemDescriptor"/>.
    /// </summary>
    public static ProblemDescriptor SystemIdNotFound { get; }
        = _factory.Create(11, HttpStatusCode.NotFound, "The Id does not refer to a Registered System.");

    /// <summary>
    /// Gets a <see cref="ProblemDescriptor"/>.
    /// </summary>
    public static ProblemDescriptor RequestCouldNotBeStored { get; }
        = _factory.Create(12, HttpStatusCode.NotFound, "An error occured when storing the Request.");
}