﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MassTransit.DapperIntegration.Tests.IntegrationTests {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Sql {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Sql() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("MassTransit.DapperIntegration.Tests.IntegrationTests.Sql", typeof(Sql).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to CREATE TABLE Jobs (
        ///    CorrelationId UUID NOT NULL,
        ///    CurrentState INT NOT NULL,    
        ///    Completed TIMESTAMP NULL,
        ///    Faulted TIMESTAMP NULL,
        ///    Started TIMESTAMP NULL,
        ///    Submitted TIMESTAMP NULL,    
        ///    EndDate TIMESTAMP WITH TIME ZONE NULL,
        ///    NextStartDate TIMESTAMP WITH TIME ZONE NULL,
        ///    StartDate TIMESTAMP WITH TIME ZONE NULL,    
        ///    AttemptId UUID NOT NULL,
        ///    JobTypeId UUID NOT NULL,
        ///    JobRetryDelayToken UUID NULL,
        ///    JobSlotWaitToken UUID NULL,    
        ///    RetryAttempt INT  [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string Postgres_CreateJobTables {
            get {
                return ResourceManager.GetString("Postgres_CreateJobTables", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to DROP TABLE IF EXISTS Jobs, JobAttempts, JobTypes;.
        /// </summary>
        internal static string Postgres_DropJobTables {
            get {
                return ResourceManager.GetString("Postgres_DropJobTables", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to TRUNCATE TABLE Jobs, JobAttempts, JobTypes;.
        /// </summary>
        internal static string Postgres_ResetJobTables {
            get {
                return ResourceManager.GetString("Postgres_ResetJobTables", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to CREATE TABLE [dbo].[Jobs] (
        ///    [CorrelationId] UNIQUEIDENTIFIER NOT NULL,
        ///    [CurrentState] INT NOT NULL,    
        ///    [Completed] DATETIME NULL,
        ///    [Faulted] DATETIME NULL,
        ///    [Started] DATETIME NULL,
        ///    [Submitted] DATETIME NULL,    
        ///    [EndDate] DATETIMEOFFSET NULL,
        ///    [NextStartDate] DATETIMEOFFSET NULL,
        ///    [StartDate] DATETIMEOFFSET NULL,    
        ///    [AttemptId] UNIQUEIDENTIFIER NOT NULL,
        ///    [JobTypeId] UNIQUEIDENTIFIER NOT NULL,
        ///    [JobRetryDelayToken] UNIQUEIDENTIFIER NULL,
        ///    [JobSlot [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string SqlServer_CreateJobTables {
            get {
                return ResourceManager.GetString("SqlServer_CreateJobTables", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to DROP TABLE IF EXISTS [dbo].[Jobs];
        ///DROP TABLE IF EXISTS [dbo].[JobAttempts];
        ///DROP TABLE IF EXISTS [dbo].[JobTypes];.
        /// </summary>
        internal static string SqlServer_DropJobTables {
            get {
                return ResourceManager.GetString("SqlServer_DropJobTables", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to TRUNCATE TABLE [dbo].[Jobs];
        ///TRUNCATE TABLE [dbo].[JobAttempts];
        ///TRUNCATE TABLE [dbo].[JobTypes];.
        /// </summary>
        internal static string SqlServer_ResetJobTables {
            get {
                return ResourceManager.GetString("SqlServer_ResetJobTables", resourceCulture);
            }
        }
    }
}
