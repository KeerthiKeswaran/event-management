using Microsoft.EntityFrameworkCore;
using Event.Models;

namespace Event.Data.Contexts
{
    public class EventDbContext : DbContext
    {
        public EventDbContext()
        {
        }

        public EventDbContext(DbContextOptions<EventDbContext> options) : base(options)
        {
        }



        public DbSet<User> Users { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Region> Regions { get; set; }
        public DbSet<UserInterestedRegion> UserInterestedRegions { get; set; }
        public DbSet<Staff> Staffs { get; set; }
        public DbSet<Venue> Venues { get; set; }
        public DbSet<VenueSeatCapacity> VenueSeatCapacities { get; set; }
        public DbSet<Event.Models.Event> Events { get; set; }
        public DbSet<EventTicketTier> EventTicketTiers { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookingDetail> BookingDetails { get; set; }
        public DbSet<BookingPayment> BookingPayments { get; set; }
        public DbSet<OrganizerUpfrontPayment> OrganizerUpfrontPayments { get; set; }
        public DbSet<OrganizerPayout> OrganizerPayouts { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<PlatformSettings> PlatformSettings { get; set; }
        public DbSet<EventStaffAllocation> EventStaffAllocations { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<AdminAction> AdminActions { get; set; }
        public DbSet<EventFeedback> EventFeedbacks { get; set; }
        public DbSet<EventReport> EventReports { get; set; }
        public DbSet<TermsAndConditions> TermsAndConditions { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Waitlist> Waitlists { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure identity sequence starts for 5-digit unique IDs (10000)
            modelBuilder.Entity<User>().Property(u => u.User_Id).UseIdentityByDefaultColumn().HasIdentityOptions(startValue: 10001);
            modelBuilder.Entity<Event.Models.Event>().Property(e => e.Event_Id).UseIdentityByDefaultColumn().HasIdentityOptions(startValue: 10000);
            modelBuilder.Entity<Booking>().Property(b => b.Booking_Id).UseIdentityByDefaultColumn().HasIdentityOptions(startValue: 10000);
            modelBuilder.Entity<Venue>().Property(v => v.Venue_Id).UseIdentityByDefaultColumn().HasIdentityOptions(startValue: 10000);
            modelBuilder.Entity<Staff>().Property(s => s.Employee_ID).UseIdentityByDefaultColumn().HasIdentityOptions(startValue: 10000);
            modelBuilder.Entity<SupportTicket>().Property(st => st.Ticket_Id).UseIdentityByDefaultColumn().HasIdentityOptions(startValue: 10000);
            modelBuilder.Entity<AdminAction>().Property(aa => aa.ActionId).UseIdentityByDefaultColumn().HasIdentityOptions(startValue: 10000);
            modelBuilder.Entity<EventFeedback>().Property(ef => ef.Feedback_Id).UseIdentityByDefaultColumn().HasIdentityOptions(startValue: 10000);
            modelBuilder.Entity<EventReport>().Property(er => er.Report_Id).UseIdentityByDefaultColumn().HasIdentityOptions(startValue: 10000);
            modelBuilder.Entity<Notification>().Property(n => n.Notification_Id).UseIdentityByDefaultColumn().HasIdentityOptions(startValue: 10000);
            modelBuilder.Entity<BookingPayment>().Property(bp => bp.Booking_Payment_Id).UseIdentityByDefaultColumn().HasIdentityOptions(startValue: 10000);
            modelBuilder.Entity<OrganizerUpfrontPayment>().Property(oup => oup.Upfront_Payment_Id).UseIdentityByDefaultColumn().HasIdentityOptions(startValue: 10000);
            modelBuilder.Entity<OrganizerPayout>().Property(op => op.Payout_Id).UseIdentityByDefaultColumn().HasIdentityOptions(startValue: 10000);
            modelBuilder.Entity<Waitlist>().Property(w => w.Waitlist_Id).UseIdentityByDefaultColumn().HasIdentityOptions(startValue: 10000);

            // Configure identity sequence start for 16-digit unique Transaction IDs
            modelBuilder.Entity<Transaction>().Property(t => t.Transaction_Id).UseIdentityByDefaultColumn().HasIdentityOptions(startValue: 1000000000000000);

            // UserInterestedRegion Composite Key & Relationships
            modelBuilder.Entity<UserInterestedRegion>()
                .HasKey(uir => new { uir.User_Id, uir.Region_Id });

            modelBuilder.Entity<UserInterestedRegion>()
                .HasOne(uir => uir.User)
                .WithMany(u => u.InterestedRegions)
                .HasForeignKey(uir => uir.User_Id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserInterestedRegion>()
                .HasOne(uir => uir.Region)
                .WithMany(r => r.InterestedUsers)
                .HasForeignKey(uir => uir.Region_Id)
                .OnDelete(DeleteBehavior.Cascade);

            // Staff Relationships
            modelBuilder.Entity<Staff>()
                .HasOne(s => s.Region)
                .WithMany(r => r.Staffs)
                .HasForeignKey(s => s.Region_Id)
                .OnDelete(DeleteBehavior.Restrict);

            // Venue Relationships
            modelBuilder.Entity<Venue>()
                .HasOne(v => v.Region)
                .WithMany(r => r.Venues)
                .HasForeignKey(v => v.Region_Id)
                .OnDelete(DeleteBehavior.Restrict);

            // VenueSeatCapacity Composite Key & Relationships
            modelBuilder.Entity<VenueSeatCapacity>()
                .HasKey(vsc => new { vsc.Venue_Id, vsc.Tier_Name });

            modelBuilder.Entity<VenueSeatCapacity>()
                .HasOne(vsc => vsc.Venue)
                .WithMany(v => v.SeatCapacities)
                .HasForeignKey(vsc => vsc.Venue_Id)
                .OnDelete(DeleteBehavior.Cascade);

            // Event Relationships
            modelBuilder.Entity<Event.Models.Event>()
                .HasOne(e => e.Organizer)
                .WithMany(u => u.OrganizedEvents)
                .HasForeignKey(e => e.Organizer_Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Event.Models.Event>()
                .HasOne(e => e.Venue)
                .WithMany(v => v.Events)
                .HasForeignKey(e => e.Venue_Id)
                .OnDelete(DeleteBehavior.SetNull);

            // Event Category and Age Category column constraints
            modelBuilder.Entity<Event.Models.Event>()
                .Property(e => e.Category)
                .HasMaxLength(100)
                .HasDefaultValue("")
                .IsRequired();

            modelBuilder.Entity<Event.Models.Event>()
                .Property(e => e.Age_Category)
                .HasMaxLength(3)
                .HasDefaultValue("ALL")
                .IsRequired();

            // EventTicketTier Composite Key & Relationships
            modelBuilder.Entity<EventTicketTier>()
                .HasKey(ett => new { ett.Event_Id, ett.Tier_Name });

            modelBuilder.Entity<EventTicketTier>()
                .HasOne(ett => ett.Event)
                .WithMany(e => e.TicketTiers)
                .HasForeignKey(ett => ett.Event_Id)
                .OnDelete(DeleteBehavior.Cascade);

            // Booking Relationships
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Attendee)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.Attendee_Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Event)
                .WithMany(e => e.Bookings)
                .HasForeignKey(b => b.Event_Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .Property(b => b.Qr_Secret_Hash)
                .HasMaxLength(255)
                .IsRequired(false);

            modelBuilder.Entity<Booking>()
                .Property(b => b.CheckIn_Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending")
                .IsRequired();

            // BookingDetail Composite Key & Relationships
            modelBuilder.Entity<BookingDetail>()
                .HasKey(bd => new { bd.Booking_Id, bd.Tier_Name });

            modelBuilder.Entity<BookingDetail>()
                .HasOne(bd => bd.Booking)
                .WithMany(b => b.Details)
                .HasForeignKey(bd => bd.Booking_Id)
                .OnDelete(DeleteBehavior.Cascade);

            // BookingPayment Relationships
            modelBuilder.Entity<BookingPayment>()
                .HasOne(bp => bp.Booking)
                .WithMany(b => b.Payments)
                .HasForeignKey(bp => bp.Booking_Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BookingPayment>()
                .HasOne(bp => bp.Transaction)
                .WithMany(t => t.BookingPayments)
                .HasForeignKey(bp => bp.Transaction_Id)
                .OnDelete(DeleteBehavior.Restrict);

            // OrganizerUpfrontPayment Relationships
            modelBuilder.Entity<OrganizerUpfrontPayment>()
                .HasOne(oup => oup.Event)
                .WithMany(e => e.UpfrontPayments)
                .HasForeignKey(oup => oup.Event_Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrganizerUpfrontPayment>()
                .HasOne(oup => oup.Transaction)
                .WithMany(t => t.UpfrontPayments)
                .HasForeignKey(oup => oup.Transaction_Id)
                .OnDelete(DeleteBehavior.Restrict);

            // OrganizerPayout Relationships
            modelBuilder.Entity<OrganizerPayout>()
                .HasOne(op => op.Event)
                .WithMany(e => e.Payouts)
                .HasForeignKey(op => op.Event_Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrganizerPayout>()
                .HasOne(op => op.Transaction)
                .WithMany(t => t.Payouts)
                .HasForeignKey(op => op.Transaction_Id)
                .OnDelete(DeleteBehavior.Restrict);

            // PlatformSettings Relationships
            modelBuilder.Entity<PlatformSettings>()
                .HasKey(ps => ps.Settings_Id);

            modelBuilder.Entity<PlatformSettings>()
                .HasOne(ps => ps.UpdatedByAdmin)
                .WithMany(a => a.UpdatedSettings)
                .HasForeignKey(ps => ps.Updated_By_Admin_Id)
                .OnDelete(DeleteBehavior.Restrict);

            // EventStaffAllocation Composite Key & Relationships
            modelBuilder.Entity<EventStaffAllocation>()
                .HasKey(esa => new { esa.Event_Id, esa.Employee_ID });

            modelBuilder.Entity<EventStaffAllocation>()
                .HasOne(esa => esa.Event)
                .WithMany(e => e.StaffAllocations)
                .HasForeignKey(esa => esa.Event_Id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EventStaffAllocation>()
                .HasOne(esa => esa.Staff)
                .WithMany(s => s.EventAllocations)
                .HasForeignKey(esa => esa.Employee_ID)
                .OnDelete(DeleteBehavior.Cascade);

            // SupportTicket Relationships
            modelBuilder.Entity<SupportTicket>()
                .HasOne(st => st.User)
                .WithMany(u => u.SupportTickets)
                .HasForeignKey(st => st.User_Id)
                .OnDelete(DeleteBehavior.Cascade);

            // AdminAction Relationships
            modelBuilder.Entity<AdminAction>()
                .HasOne(aa => aa.Admin)
                .WithMany()
                .HasForeignKey(aa => aa.AdminId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AdminAction>()
                .HasOne(aa => aa.SupportTicket)
                .WithMany()
                .HasForeignKey(aa => aa.TicketId)
                .OnDelete(DeleteBehavior.SetNull);

            // EventFeedback Relationships
            modelBuilder.Entity<EventFeedback>()
                .HasOne(ef => ef.Event)
                .WithMany(e => e.Feedbacks)
                .HasForeignKey(ef => ef.Event_Id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EventFeedback>()
                .HasOne(ef => ef.Attendee)
                .WithMany(u => u.Feedbacks)
                .HasForeignKey(ef => ef.Attendee_Id)
                .OnDelete(DeleteBehavior.Cascade);

            // EventReport Relationships
            modelBuilder.Entity<EventReport>()
                .HasOne(er => er.Event)
                .WithMany(e => e.Reports)
                .HasForeignKey(er => er.Event_Id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EventReport>()
                .HasOne(er => er.Reporter)
                .WithMany(u => u.Reports)
                .HasForeignKey(er => er.Reporter_Id)
                .OnDelete(DeleteBehavior.Cascade);

            // Waitlist Relationships
            modelBuilder.Entity<Waitlist>()
                .HasOne(w => w.Event)
                .WithMany()
                .HasForeignKey(w => w.Event_Id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Waitlist>()
                .HasOne(w => w.Attendee)
                .WithMany()
                .HasForeignKey(w => w.Attendee_Id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Waitlist>()
                .HasOne(w => w.Booking)
                .WithMany()
                .HasForeignKey(w => w.Booking_Id)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Waitlist>()
                .HasIndex(w => new { w.Event_Id, w.Tier_Name, w.Status, w.Position });

            // User to TermsAndConditions Relationship
            modelBuilder.Entity<User>()
                .Ignore(u => u.ConsentedTerms);

            modelBuilder.Entity<User>()
                .Property(u => u.Password_Reset_Token)
                .HasMaxLength(255)
                .IsRequired(false);

            modelBuilder.Entity<Admin>()
                .Property(a => a.Email)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<Admin>()
                .Property(a => a.Password_Reset_Token)
                .HasMaxLength(255)
                .IsRequired(false);
        }
    }
}
