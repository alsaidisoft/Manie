﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SecurityClearanceWebApp.Models
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class RA42_SECURITY_CLEARANCEEntities : DbContext
    {
        public RA42_SECURITY_CLEARANCEEntities()
            : base("name=RA42_SECURITY_CLEARANCEEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<RA42_ACCESS_SELECT_MST> RA42_ACCESS_SELECT_MST { get; set; }
        public virtual DbSet<RA42_ACCESS_TYPE_MST> RA42_ACCESS_TYPE_MST { get; set; }
        public virtual DbSet<RA42_AIR_CREW_PASS_DTL> RA42_AIR_CREW_PASS_DTL { get; set; }
        public virtual DbSet<RA42_ANNOUNCEMENT_MST> RA42_ANNOUNCEMENT_MST { get; set; }
        public virtual DbSet<RA42_API> RA42_API { get; set; }
        public virtual DbSet<RA42_AUTHORAIZATION_SIGNAURE_MST> RA42_AUTHORAIZATION_SIGNAURE_MST { get; set; }
        public virtual DbSet<RA42_AUTHORIZATION_PASS_DTL> RA42_AUTHORIZATION_PASS_DTL { get; set; }
        public virtual DbSet<RA42_BLOOD_TYPE_MST> RA42_BLOOD_TYPE_MST { get; set; }
        public virtual DbSet<RA42_CARD_DESIGN_DTL> RA42_CARD_DESIGN_DTL { get; set; }
        public virtual DbSet<RA42_CARD_FOR_MST> RA42_CARD_FOR_MST { get; set; }
        public virtual DbSet<RA42_COMMENTS_MST> RA42_COMMENTS_MST { get; set; }
        public virtual DbSet<RA42_COMPANY_MST> RA42_COMPANY_MST { get; set; }
        public virtual DbSet<RA42_COMPANY_PASS_DTL> RA42_COMPANY_PASS_DTL { get; set; }
        public virtual DbSet<RA42_COMPANY_TYPE_MST> RA42_COMPANY_TYPE_MST { get; set; }
        public virtual DbSet<RA42_CONTRACTING_COMPANIES_PASS_DTL> RA42_CONTRACTING_COMPANIES_PASS_DTL { get; set; }
        public virtual DbSet<RA42_DOCUMENTS_ACCESS_MST> RA42_DOCUMENTS_ACCESS_MST { get; set; }
        public virtual DbSet<RA42_ENTER_EXIT_REGISTERATION_DTL> RA42_ENTER_EXIT_REGISTERATION_DTL { get; set; }
        public virtual DbSet<RA42_EVENT_EXERCISE_MST> RA42_EVENT_EXERCISE_MST { get; set; }
        public virtual DbSet<RA42_EVENTS_TYPE_MST> RA42_EVENTS_TYPE_MST { get; set; }
        public virtual DbSet<RA42_FAMILY_PASS_DTL> RA42_FAMILY_PASS_DTL { get; set; }
        public virtual DbSet<RA42_FILE_CARD_MST> RA42_FILE_CARD_MST { get; set; }
        public virtual DbSet<RA42_FILE_FORCES_MST> RA42_FILE_FORCES_MST { get; set; }
        public virtual DbSet<RA42_FILE_NOTE_TYPE_MST> RA42_FILE_NOTE_TYPE_MST { get; set; }
        public virtual DbSet<RA42_FILE_TYPE_MST> RA42_FILE_TYPE_MST { get; set; }
        public virtual DbSet<RA42_FILES_MST> RA42_FILES_MST { get; set; }
        public virtual DbSet<RA42_FORCES_MST> RA42_FORCES_MST { get; set; }
        public virtual DbSet<RA42_GENDERS_MST> RA42_GENDERS_MST { get; set; }
        public virtual DbSet<RA42_GENERAL_USERS_MST> RA42_GENERAL_USERS_MST { get; set; }
        public virtual DbSet<RA42_IDENTITY_MST> RA42_IDENTITY_MST { get; set; }
        public virtual DbSet<RA42_MANAGER_SIGNATURE_MST> RA42_MANAGER_SIGNATURE_MST { get; set; }
        public virtual DbSet<RA42_MEMBERS_DTL> RA42_MEMBERS_DTL { get; set; }
        public virtual DbSet<RA42_PASS_TYPE_MST> RA42_PASS_TYPE_MST { get; set; }
        public virtual DbSet<RA42_PERMITS_DTL> RA42_PERMITS_DTL { get; set; }
        public virtual DbSet<RA42_PLATE_CHAR_MST> RA42_PLATE_CHAR_MST { get; set; }
        public virtual DbSet<RA42_PLATE_TYPE_MST> RA42_PLATE_TYPE_MST { get; set; }
        public virtual DbSet<RA42_PRINT_MST> RA42_PRINT_MST { get; set; }
        public virtual DbSet<RA42_RELATIVE_MST> RA42_RELATIVE_MST { get; set; }
        public virtual DbSet<RA42_RELATIVE_TYPE_MST> RA42_RELATIVE_TYPE_MST { get; set; }
        public virtual DbSet<RA42_REPORTS_MST> RA42_REPORTS_MST { get; set; }
        public virtual DbSet<RA42_SEARCH_MST> RA42_SEARCH_MST { get; set; }
        public virtual DbSet<RA42_SECTIONS_MST> RA42_SECTIONS_MST { get; set; }
        public virtual DbSet<RA42_SECURITY_CAVEATES_DTL> RA42_SECURITY_CAVEATES_DTL { get; set; }
        public virtual DbSet<RA42_SECURITY_PASS_DTL> RA42_SECURITY_PASS_DTL { get; set; }
        public virtual DbSet<RA42_SLIDER_DTL> RA42_SLIDER_DTL { get; set; }
        public virtual DbSet<RA42_STATIONS_MST> RA42_STATIONS_MST { get; set; }
        public virtual DbSet<RA42_TEMPRORY_COMPANY_PASS_DTL> RA42_TEMPRORY_COMPANY_PASS_DTL { get; set; }
        public virtual DbSet<RA42_TRAINEES_PASS_DTL> RA42_TRAINEES_PASS_DTL { get; set; }
        public virtual DbSet<RA42_TRANSACTION_DTL> RA42_TRANSACTION_DTL { get; set; }
        public virtual DbSet<RA42_TRANSACTION_TYPE_MST> RA42_TRANSACTION_TYPE_MST { get; set; }
        public virtual DbSet<RA42_VECHILE_CATIGORY_MST> RA42_VECHILE_CATIGORY_MST { get; set; }
        public virtual DbSet<RA42_VECHILE_COLOR_MST> RA42_VECHILE_COLOR_MST { get; set; }
        public virtual DbSet<RA42_VECHILE_NAME_MST> RA42_VECHILE_NAME_MST { get; set; }
        public virtual DbSet<RA42_VECHILE_PASS_DTL> RA42_VECHILE_PASS_DTL { get; set; }
        public virtual DbSet<RA42_VECHILE_VIOLATION_DTL> RA42_VECHILE_VIOLATION_DTL { get; set; }
        public virtual DbSet<RA42_VIOLATIONS_MST> RA42_VIOLATIONS_MST { get; set; }
        public virtual DbSet<RA42_VISITOR_MST> RA42_VISITOR_MST { get; set; }
        public virtual DbSet<RA42_VISITOR_PASS_DTL> RA42_VISITOR_PASS_DTL { get; set; }
        public virtual DbSet<RA42_WORKFLOW_MST> RA42_WORKFLOW_MST { get; set; }
        public virtual DbSet<RA42_WORKFLOW_RESPONSIBLE_MST> RA42_WORKFLOW_RESPONSIBLE_MST { get; set; }
        public virtual DbSet<RA42_ZONE_AREA_MST> RA42_ZONE_AREA_MST { get; set; }
        public virtual DbSet<RA42_ZONE_MASTER_MST> RA42_ZONE_MASTER_MST { get; set; }
        public virtual DbSet<RA42_ZONE_SUB_AREA_MST> RA42_ZONE_SUB_AREA_MST { get; set; }
        public virtual DbSet<sysdiagram> sysdiagrams { get; set; }
    }
}
