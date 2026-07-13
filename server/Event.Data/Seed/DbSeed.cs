using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Event.Data.Contexts;
using Event.Models;

using Event.Contracts.IServices;

namespace Event.Data.Seed
{
    public static class DbSeed
    {
        public static async Task SeedAsync(EventDbContext context, IFileStorageService storageService)
        {
            Console.WriteLine("Applying database migrations...");
            await context.Database.MigrateAsync();
            Console.WriteLine("Database migrations applied successfully.");
            #region Clean Existing Data
            // =========================================================================
            // 0. CLEAN EXISTING DATA (Idempotent execution)
            // =========================================================================
            string truncateSql = @"
                DO $$ 
                DECLARE 
                    r RECORD;
                BEGIN
                    FOR r IN (
                        SELECT tablename 
                        FROM pg_tables 
                        WHERE schemaname = 'public' 
                          AND tablename NOT IN ('__EFMigrationsHistory', '__efmigrationshistory')
                    ) LOOP
                        EXECUTE format('TRUNCATE TABLE %I CASCADE;', r.tablename);
                    END LOOP;
                END $$;";

            await context.Database.ExecuteSqlRawAsync(truncateSql);
            #endregion

            #region Seed Regions (Table: Management)
            // =========================================================================
            // 1. SEED REGIONS (Table: "Management")
            // Focus: 50% Tamil Nadu (6), 50% Rest of India (6)
            // =========================================================================
            var regions = new List<Region>
            {
                new Region { Region_Id = "REG01", Region_Name = "Chennai", No_Of_Staffs = 100 },
                new Region { Region_Id = "REG02", Region_Name = "Coimbatore", No_Of_Staffs = 80 },
                new Region { Region_Id = "REG03", Region_Name = "Madurai", No_Of_Staffs = 70 },
                new Region { Region_Id = "REG04", Region_Name = "Trichy", No_Of_Staffs = 60 },
                new Region { Region_Id = "REG14", Region_Name = "Salem", No_Of_Staffs = 50 },
                new Region { Region_Id = "REG15", Region_Name = "Tirunelveli", No_Of_Staffs = 50 },
                new Region { Region_Id = "REG07", Region_Name = "Bengaluru", No_Of_Staffs = 80 },
                new Region { Region_Id = "REG05", Region_Name = "Mumbai", No_Of_Staffs = 100 },
                new Region { Region_Id = "REG06", Region_Name = "Delhi", No_Of_Staffs = 100 },
                new Region { Region_Id = "REG08", Region_Name = "Hyderabad", No_Of_Staffs = 80 },
                new Region { Region_Id = "REG09", Region_Name = "Kochi", No_Of_Staffs = 50 },
                new Region { Region_Id = "REG10", Region_Name = "Kolkata", No_Of_Staffs = 70 }
            };
            await context.Regions.AddRangeAsync(regions);
            await context.SaveChangesAsync();
            #endregion

            #region Seed Terms and Conditions (Table: TermsAndConditions)
            // =========================================================================
            // 2. SEED TERMS & CONDITIONS (Table: "TermsAndConditions")
            // =========================================================================
            var terms = new List<TermsAndConditions>
            {
                new TermsAndConditions { Terms_Id = "G10001", Version = "v1.0", File_Path = "/assets/policies/G10001.md", Type = "General", Is_Active = true, Created_At = DateTime.UtcNow },
                new TermsAndConditions { Terms_Id = "E10001", Version = "v1.0", File_Path = "/assets/policies/E10001.md", Type = "EventCreation", Is_Active = true, Created_At = DateTime.UtcNow },
                new TermsAndConditions { Terms_Id = "C10001", Version = "v1.0", File_Path = "/assets/policies/C10001.md", Type = "Cancellation", Is_Active = true, Created_At = DateTime.UtcNow }
            };
            await context.TermsAndConditions.AddRangeAsync(terms);
            await context.SaveChangesAsync();
            #endregion

            #region Seed Administrators (Table: Admins)
            // =========================================================================
            // 3. SEED ADMINISTRATORS (Table: "Admins")
            // Password for ADM01 is 'AdminPassword123!'
            // Password for FIN01 is 'FinancePassword123!'
            // =========================================================================
            var admins = new List<Admin>
            {
                new Admin { Admin_Id = "ADM01", Name = "System Administrator", Email = "keerthiipsedits@gmail.com", Password_Hash = "61eJPjKCSth/N5T72cAbXmAqrhLOyHLRuTnmKG1jGO3eu/Fwp2nldfpJ9UOW4S3p" },
                new Admin { Admin_Id = "FIN01", Name = "Finance Executive", Email = "fuelgrad@gmail.com", Password_Hash = "EBNGxyXSoaYbAAWAjCKcFG4PoXc7YgGT8nZ/QEnU3A8Jnj9lO3gkeLmfVHrx3QWG" }
            };
            await context.Admins.AddRangeAsync(admins);
            await context.SaveChangesAsync();
            #endregion

            #region Seed Platform Settings (Table: PlatformSettings)
            // =========================================================================
            // 4. SEED PLATFORM SETTINGS (Table: "PlatformSettings")
            // =========================================================================
            var settings = new PlatformSettings
            {
                Settings_Id = 1,
                Staff_Flat_Rate = 500.00m,
                Virtual_Event_Activation_Fee = 500.00m,
                Physical_Event_Activation_Fee = 2000.00m,
                Ticket_Commission_Percentage = 3.50m,
                Ticket_Fixed_Fee = 20.00m,
                Max_Tickets_Per_Booking = 10,
                Updated_At = DateTime.UtcNow,
                Updated_By_Admin_Id = "ADM01"
            };
            await context.PlatformSettings.AddAsync(settings);
            await context.SaveChangesAsync();
            #endregion

            #region Seed Active Dummy Users (Table: Users)
            // =========================================================================
            // 5. SEED ACTIVE DUMMY USERS (Table: "Users")
            // 30 dummy users starting from ID 10001
            // Organizers (10001, 10002, 10007) have E10001 appended to their consent terms.
            // Password hash for all is 'SecurePassword123!'
            // =========================================================================
            var users = new List<User>();
            string[] userNames = {
                "Amit Sharma", "Priya Patel", "Rajesh Kumar", "Anjali Singh", "Rohan Nair",
                "Meera Iyer", "Suresh Babu", "Deepa Ram", "Karthik S", "Divya K",
                "Vikram Aditya", "Neha Gupta", "Sandeep Verma", "Pooja Sharma", "Rahul Singh",
                "Sneha Patil", "Karan Johar", "Shreya Ghoshal", "Arjun Kapoor", "Shruti Haasan",
                "Vijay Joseph", "Ajith Kumar", "Surya Sivakumar", "Karthi Sivakumar", "Vikram Kennedy",
                "Dhanush Raja", "Simbu Silambarasan", "Sivakarthikeyan Doss", "Trisha Krishnan", "Nayanthara Kurian"
            };

            for (int i = 0; i < userNames.Length; i++)
            {
                int userId = 10001 + i;
                string email = $"{userNames[i].Replace(" ", ".").ToLower()}@example.com";
                bool isOrganizer = (userId == 10001 || userId == 10002 || userId == 10007);
                string consentedTerms = isOrganizer ? "G10001E10001" : "G10001";

                users.Add(new User
                {
                    User_Id = userId,
                    Name = userNames[i],
                    Email = email,
                    Mobile_Number = $"98765432{(i + 1):D2}",
                    Password_Hash = "61eJPjKCSth/N5T72cAbXmAqrhLOyHLRuTnmKG1jGO3eu/Fwp2nldfpJ9UOW4S3p",
                    Has_Marketing_Consent = false,
                    Consented_Terms_Id = consentedTerms,
                    Status = "Active"
                });
            }
            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();
            #endregion

            #region Seed User Interested Regions (Table: UserInterestedRegions)
            // =========================================================================
            // 6. SEED USER INTERESTED REGIONS (Table: "UserInterestedRegions")
            // Map exactly 1 region for each user
            // =========================================================================
            var userInterestedRegions = new List<UserInterestedRegion>();
            for (int i = 0; i < users.Count; i++)
            {
                var user = users[i];
                var regionIndex = i % regions.Count;
                userInterestedRegions.Add(new UserInterestedRegion { User_Id = user.User_Id, Region_Id = regions[regionIndex].Region_Id });
            }
            await context.UserInterestedRegions.AddRangeAsync(userInterestedRegions);
            await context.SaveChangesAsync();
            #endregion

            #region Seed Staffs (Table: Staffs)
            // =========================================================================
            // 7. SEED OPERATIONAL STAFFS (Table: "Staffs")
            // 100 staffs distributed among the 12 regions with IsAllocated = false
            // =========================================================================
            var staffs = new List<Staff>();
            string[] staffFirstNames = { "Aarav", "Vihaan", "Vivaan", "Ananya", "Diya", "Saisha", "Aditya", "Ishaan", "Aanya", "Aaradhya", "Pranav", "Siddharth", "Rohan", "Neha", "Pooja", "Rahul", "Arjun", "Kabir", "Riya", "Shruti", "Karan", "Shreya", "Sneha", "Deepak", "Sanjay", "Vikram", "Anil", "Sunil", "Vijay", "Rajesh", "Amit", "Priya", "Suresh", "Divya", "Karthik" };
            string[] staffLastNames = { "Sharma", "Verma", "Kumar", "Singh", "Patel", "Joshi", "Mehta", "Nair", "Pillai", "Rao", "Iyer", "Reddy", "Choudhury", "Gupta", "Sen", "Das" };

            int employeeIdCounter = 20001;
            foreach (var region in regions)
            {
                for (int i = 1; i <= region.No_Of_Staffs; i++)
                {
                    int empId = employeeIdCounter++;
                    var fn = staffFirstNames[(empId * 3) % staffFirstNames.Length];
                    var ln = staffLastNames[(empId * 7) % staffLastNames.Length];
                    staffs.Add(new Staff
                    {
                        Employee_ID = empId,
                        Name = $"{fn} {ln}",
                        Email = $"{fn.ToLower()}.{ln.ToLower()}{empId}@example.com",
                        Region_Id = region.Region_Id,
                        IsAllocated = false
                    });
                }
            }
            await context.Staffs.AddRangeAsync(staffs);
            await context.SaveChangesAsync();
            #endregion

            #region Seed Venues and Capacities (Tables: Venues, VenueSeatCapacities)
            // =========================================================================
            // 8. SEED VENUES & SEAT CAPACITIES (Table: "Venues" & "VenueSeatCapacities")
            // =========================================================================
            var venues = new List<Venue>
            {
                new Venue { Venue_Id = 10001, Name = "Music Academy Hall", Region_Id = "REG01", Hourly_Price = 500m, Is_Available = true, Address = "Cathedral Road, Chennai" },
                new Venue { Venue_Id = 10002, Name = "Chennai Trade Centre", Region_Id = "REG01", Hourly_Price = 800m, Is_Available = true, Address = "Nandambakkam, Chennai" },
                new Venue { Venue_Id = 10003, Name = "Island Grounds", Region_Id = "REG01", Hourly_Price = 600m, Is_Available = true, Address = "The Island, Chennai" },
                new Venue { Venue_Id = 10004, Name = "Sathyam Cinemas Complex", Region_Id = "REG01", Hourly_Price = 400m, Is_Available = true, Address = "Royapettah, Chennai" },
                new Venue { Venue_Id = 10005, Name = "YMCA Grounds Nandanam", Region_Id = "REG01", Hourly_Price = 1000m, Is_Available = true, Address = "Nandanam, Chennai" },
                new Venue { Venue_Id = 10006, Name = "Kamarajar Memorial Hall", Region_Id = "REG01", Hourly_Price = 600m, Is_Available = true, Address = "Teynampet, Chennai" },
                new Venue { Venue_Id = 10007, Name = "Kovalam Beach Point", Region_Id = "REG01", Hourly_Price = 400m, Is_Available = true, Address = "ECR, Kovalam, Chennai" },
                new Venue { Venue_Id = 10008, Name = "IITM ICSR Auditorium", Region_Id = "REG01", Hourly_Price = 500m, Is_Available = true, Address = "IIT Madras Campus, Chennai" },
                new Venue { Venue_Id = 10009, Name = "CODISSIA Hall A", Region_Id = "REG02", Hourly_Price = 700m, Is_Available = true, Address = "Avinashi Road, Coimbatore" },
                new Venue { Venue_Id = 10010, Name = "VOC Park Grounds", Region_Id = "REG02", Hourly_Price = 300m, Is_Available = true, Address = "VOC Park, Coimbatore" },
                new Venue { Venue_Id = 10011, Name = "Suntec Exhibition Centre", Region_Id = "REG02", Hourly_Price = 1200m, Is_Available = true, Address = "Gandhipuram, Coimbatore" },
                new Venue { Venue_Id = 10012, Name = "Gandhipuram Cultural Center", Region_Id = "REG02", Hourly_Price = 400m, Is_Available = true, Address = "Gandhipuram, Coimbatore" },
                new Venue { Venue_Id = 10013, Name = "Coimbatore Kalaiarangam", Region_Id = "REG02", Hourly_Price = 500m, Is_Available = true, Address = "DB Road, Coimbatore" },
                new Venue { Venue_Id = 10014, Name = "Coimbatore Botanical Club", Region_Id = "REG02", Hourly_Price = 300m, Is_Available = true, Address = "Marudhamalai Road, Coimbatore" },
                new Venue { Venue_Id = 10015, Name = "Tamukkam Grounds", Region_Id = "REG03", Hourly_Price = 400m, Is_Available = true, Address = "Tallakulam, Madurai" },
                new Venue { Venue_Id = 10016, Name = "Meenakshi Temple Convention Hall", Region_Id = "REG03", Hourly_Price = 500m, Is_Available = true, Address = "Near Temple, Madurai" },
                new Venue { Venue_Id = 10017, Name = "Madurai Palace Grounds", Region_Id = "REG03", Hourly_Price = 600m, Is_Available = true, Address = "East Gate, Madurai" },
                new Venue { Venue_Id = 10018, Name = "K.K. Nagar Exhibition Ground", Region_Id = "REG03", Hourly_Price = 300m, Is_Available = true, Address = "KK Nagar, Madurai" },
                new Venue { Venue_Id = 10019, Name = "Sangam Hotel Conference Hall", Region_Id = "REG04", Hourly_Price = 600m, Is_Available = true, Address = "Collector Office Road, Trichy" },
                new Venue { Venue_Id = 10020, Name = "National College Grounds", Region_Id = "REG04", Hourly_Price = 300m, Is_Available = true, Address = "Dindigul Road, Trichy" },
                new Venue { Venue_Id = 10021, Name = "Trichy Arts Auditorium", Region_Id = "REG04", Hourly_Price = 400m, Is_Available = true, Address = "Cantonment, Trichy" },
                new Venue { Venue_Id = 10022, Name = "Rockfort Temple Steps Entrance", Region_Id = "REG04", Hourly_Price = 200m, Is_Available = true, Address = "Rockfort, Trichy" },
                new Venue { Venue_Id = 10023, Name = "NIT Trichy Convention Hall", Region_Id = "REG04", Hourly_Price = 700m, Is_Available = true, Address = "Thanjavur Road, Trichy" },
                new Venue { Venue_Id = 10024, Name = "Salem Corporation Exhibition Ground", Region_Id = "REG14", Hourly_Price = 300m, Is_Available = true, Address = "Sarda College Road, Salem" },
                new Venue { Venue_Id = 10025, Name = "Yercaud Estate Viewpoint Cafe", Region_Id = "REG14", Hourly_Price = 400m, Is_Available = true, Address = "Yercaud Hills, Salem" },
                new Venue { Venue_Id = 10026, Name = "Salem Town Hall Grounds", Region_Id = "REG14", Hourly_Price = 300m, Is_Available = true, Address = "Town Hall, Salem" },
                new Venue { Venue_Id = 10027, Name = "Nellai Exhibition Ground", Region_Id = "REG15", Hourly_Price = 300m, Is_Available = true, Address = "Vannarpettai, Tirunelveli" },
                new Venue { Venue_Id = 10028, Name = "Courtallam Main Falls Pavilion", Region_Id = "REG15", Hourly_Price = 400m, Is_Available = true, Address = "Courtallam, Tirunelveli" },
                new Venue { Venue_Id = 10029, Name = "Nellaiappar Sannadhi Street", Region_Id = "REG15", Hourly_Price = 200m, Is_Available = true, Address = "Nellaiappar Temple, Tirunelveli" },
                new Venue { Venue_Id = 10030, Name = "Jio World Convention Centre", Region_Id = "REG05", Hourly_Price = 1500m, Is_Available = true, Address = "BKC, Mumbai" },
                new Venue { Venue_Id = 10031, Name = "NCPA Nariman Point", Region_Id = "REG05", Hourly_Price = 1000m, Is_Available = true, Address = "Nariman Point, Mumbai" },
                new Venue { Venue_Id = 10032, Name = "Gateway Entrance Plaza", Region_Id = "REG05", Hourly_Price = 500m, Is_Available = true, Address = "Apollo Bandar, Mumbai" },
                new Venue { Venue_Id = 10033, Name = "Jawaharlal Nehru Stadium", Region_Id = "REG06", Hourly_Price = 1200m, Is_Available = true, Address = "Pragati Vihar, Delhi" },
                new Venue { Venue_Id = 10034, Name = "Pragati Maidan Hall 5", Region_Id = "REG06", Hourly_Price = 1500m, Is_Available = true, Address = "Pragati Maidan, Delhi" },
                new Venue { Venue_Id = 10035, Name = "Qutub Minar Complex Grounds", Region_Id = "REG06", Hourly_Price = 800m, Is_Available = true, Address = "Mehrauli, Delhi" },
                new Venue { Venue_Id = 10036, Name = "Chinnaswamy Stadium Club Hall", Region_Id = "REG07", Hourly_Price = 1000m, Is_Available = true, Address = "MG Road, Bengaluru" },
                new Venue { Venue_Id = 10037, Name = "Lalbagh Glass House", Region_Id = "REG07", Hourly_Price = 800m, Is_Available = true, Address = "Lalbagh, Bengaluru" },
                new Venue { Venue_Id = 10038, Name = "Nandi Hills Foothills Gate", Region_Id = "REG07", Hourly_Price = 500m, Is_Available = true, Address = "Nandi Hills, Bengaluru" },
                new Venue { Venue_Id = 10039, Name = "Shilpakala Vedika", Region_Id = "REG08", Hourly_Price = 800m, Is_Available = true, Address = "Madhapur, Hyderabad" },
                new Venue { Venue_Id = 10040, Name = "HITEX Exhibition Centre", Region_Id = "REG08", Hourly_Price = 1200m, Is_Available = true, Address = "Kothaguda, Hyderabad" },
                new Venue { Venue_Id = 10041, Name = "Golconda Fort Inner Court", Region_Id = "REG08", Hourly_Price = 700m, Is_Available = true, Address = "Golconda, Hyderabad" },
                new Venue { Venue_Id = 10042, Name = "Bolgatty Palace Grounds", Region_Id = "REG09", Hourly_Price = 800m, Is_Available = true, Address = "Mulavukad, Kochi" },
                new Venue { Venue_Id = 10043, Name = "Aspinwall House Fort Kochi", Region_Id = "REG09", Hourly_Price = 600m, Is_Available = true, Address = "Fort Kochi, Kochi" },
                new Venue { Venue_Id = 10044, Name = "Fort Kochi Beach Walkway", Region_Id = "REG09", Hourly_Price = 400m, Is_Available = true, Address = "Fort Kochi, Kochi" },
                new Venue { Venue_Id = 10045, Name = "Rabindra Sadan Hall", Region_Id = "REG10", Hourly_Price = 700m, Is_Available = true, Address = "AJC Bose Road, Kolkata" },
                new Venue { Venue_Id = 10046, Name = "Red Road Exhibition Boulevard", Region_Id = "REG10", Hourly_Price = 800m, Is_Available = true, Address = "Red Road, Kolkata" },
                new Venue { Venue_Id = 10047, Name = "Central Park Fair Ground Salt Lake", Region_Id = "REG10", Hourly_Price = 600m, Is_Available = true, Address = "Salt Lake, Kolkata" },
                new Venue { Venue_Id = 10048, Name = "St. George Community Center", Region_Id = "REG01", Hourly_Price = 300m, Is_Available = true, Address = "12 Cathedral Road, Chennai" },
                new Venue { Venue_Id = 10049, Name = "Victoria Memorial Gardens", Region_Id = "REG01", Hourly_Price = 300m, Is_Available = true, Address = "Pantheon Road, Egmore, Chennai" },
                new Venue { Venue_Id = 10050, Name = "Nehru Exhibition Plaza", Region_Id = "REG02", Hourly_Price = 350m, Is_Available = true, Address = "Avinashi Road, Coimbatore" },
                new Venue { Venue_Id = 10051, Name = "Greenwood Civic Hall", Region_Id = "REG02", Hourly_Price = 250m, Is_Available = true, Address = "RS Puram, Coimbatore" },
                new Venue { Venue_Id = 10052, Name = "Royal Heritage Palace", Region_Id = "REG03", Hourly_Price = 400m, Is_Available = true, Address = "Palace Road, Madurai" },
                new Venue { Venue_Id = 10053, Name = "Lakeside Amphitheater", Region_Id = "REG03", Hourly_Price = 300m, Is_Available = true, Address = "Vandiyur Lakefront, Madurai" },
                new Venue { Venue_Id = 10054, Name = "Metro Business Hub", Region_Id = "REG04", Hourly_Price = 350m, Is_Available = true, Address = "Cantonment, Trichy" },
                new Venue { Venue_Id = 10055, Name = "Town Square Pavilion", Region_Id = "REG04", Hourly_Price = 250m, Is_Available = true, Address = "Thillai Nagar, Trichy" },
                new Venue { Venue_Id = 10056, Name = "Ocean Breeze Arena", Region_Id = "REG14", Hourly_Price = 300m, Is_Available = true, Address = "Steel Plant Road, Salem" },
                new Venue { Venue_Id = 10057, Name = "Himalayan View Retreat", Region_Id = "REG14", Hourly_Price = 450m, Is_Available = true, Address = "Pagoda Point Road, Yercaud, Salem" },
                new Venue { Venue_Id = 10058, Name = "Valley Seminar Hall", Region_Id = "REG15", Hourly_Price = 300m, Is_Available = true, Address = "High Road, Tirunelveli" },
                new Venue { Venue_Id = 10059, Name = "Pioneer Tech Center", Region_Id = "REG15", Hourly_Price = 350m, Is_Available = true, Address = "Palayamkottai, Tirunelveli" },
                new Venue { Venue_Id = 10060, Name = "Orchard Banquet Suite", Region_Id = "REG05", Hourly_Price = 600m, Is_Available = true, Address = "Andheri East, Mumbai" },
                new Venue { Venue_Id = 10061, Name = "Sunrise Sports Complex", Region_Id = "REG05", Hourly_Price = 450m, Is_Available = true, Address = "Worli Seaface, Mumbai" },
                new Venue { Venue_Id = 10062, Name = "Summit Conference Room", Region_Id = "REG06", Hourly_Price = 500m, Is_Available = true, Address = "Connaught Place, New Delhi" },
                new Venue { Venue_Id = 10063, Name = "Palm Grove Resort", Region_Id = "REG06", Hourly_Price = 700m, Is_Available = true, Address = "Chhatarpur, New Delhi" },
                new Venue { Venue_Id = 10064, Name = "Highland Club House", Region_Id = "REG07", Hourly_Price = 400m, Is_Available = true, Address = "Indiranagar, Bengaluru" },
                new Venue { Venue_Id = 10065, Name = "Sapphire Exhibition Hall", Region_Id = "REG07", Hourly_Price = 550m, Is_Available = true, Address = "Whitefield, Bengaluru" },
                new Venue { Venue_Id = 10066, Name = "Emerald Banquet Plaza", Region_Id = "REG08", Hourly_Price = 450m, Is_Available = true, Address = "Gachibowli, Hyderabad" },
                new Venue { Venue_Id = 10067, Name = "Jasmine Meeting Hub", Region_Id = "REG08", Hourly_Price = 350m, Is_Available = true, Address = "Banjara Hills, Hyderabad" }
            };
            await context.Venues.AddRangeAsync(venues);
            await context.SaveChangesAsync();

            var seatCapacities = new List<VenueSeatCapacity>();
            foreach (var v in venues)
            {
                seatCapacities.Add(new VenueSeatCapacity { Venue_Id = v.Venue_Id, Tier_Name = "General Admission", Total_Seats = 500 });
                seatCapacities.Add(new VenueSeatCapacity { Venue_Id = v.Venue_Id, Tier_Name = "VIP Access", Total_Seats = 50 });
            }
            await context.VenueSeatCapacities.AddRangeAsync(seatCapacities);
            await context.SaveChangesAsync();
            #endregion

            #region Seed Events (Tables: Events, EventTicketTiers)
            // =========================================================================
            // 9. SEED EVENTS (Table: "Events" & "EventTicketTiers")
            // 48 events mapping directly from client mock events.
            // Description URLs will store the absolute path of written text files.
            // =========================================================================
            string rootPath = Directory.GetCurrentDirectory().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (rootPath.Contains("bin"))
            {
                rootPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            else if (rootPath.EndsWith("Event.API") || rootPath.EndsWith("Event.Business.Tests") || rootPath.EndsWith("Event.Business") || rootPath.EndsWith("Event.Data"))
            {
                rootPath = Path.GetFullPath(Path.Combine(rootPath, "..")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            var eventDefs = GetEventDefinitions();
            var seededEvents = new List<Event.Models.Event>();
            var ticketTiersToSeed = new List<EventTicketTier>();

            foreach (var def in eventDefs)
            {
                int eventId = def.EventId;

                // Determine Organizer ID
                int organizerId = 10001; // default Amit Sharma
                if (def.RegionId == "CBE23" || def.RegionId == "MUM22" || def.RegionId == "HYD99" || def.RegionId == "SLM33")
                {
                    organizerId = 10002; // Priya Patel
                }
                else if (def.RegionId == "MDU45" || def.RegionId == "DEL11" || def.RegionId == "KOC44" || def.RegionId == "TNV88")
                {
                    organizerId = 10007; // Suresh Babu
                }

                // Map Venue
                int? venueId = null;
                if (def.EventType != "Virtual" && !string.IsNullOrEmpty(def.VenueName))
                {
                    var matchingVenue = venues.FirstOrDefault(v => v.Name.Equals(def.VenueName, StringComparison.OrdinalIgnoreCase));
                    if (matchingVenue != null)
                    {
                        venueId = matchingVenue.Venue_Id;
                        matchingVenue.Is_Available = false; // Mark as allocated
                    }
                    else
                    {
                        // Create a new venue and mark as allocated
                        int newVenueId = venues.Any() ? venues.Max(v => v.Venue_Id) + 1 : 10001;
                        var newVenue = new Venue
                        {
                            Venue_Id = newVenueId,
                            Name = def.VenueName,
                            Region_Id = def.RegionId,
                            Hourly_Price = 500m,
                            Is_Available = false, // marked as allocated
                            Address = $"Address for {def.VenueName}, {def.RegionId}"
                        };
                        venues.Add(newVenue);
                        await context.Venues.AddAsync(newVenue);

                        // Add seat capacities for it
                        var capacities = new List<VenueSeatCapacity>
                        {
                            new VenueSeatCapacity { Venue_Id = newVenue.Venue_Id, Tier_Name = "General Admission", Total_Seats = 500 },
                            new VenueSeatCapacity { Venue_Id = newVenue.Venue_Id, Tier_Name = "VIP Access", Total_Seats = 50 }
                        };
                        await context.VenueSeatCapacities.AddRangeAsync(capacities);

                        venueId = newVenueId;
                    }
                }

                // Write description file and get absolute path
                string descriptionFilePath = await CreateDescriptionFile(storageService, eventId, def.Description);

                var newEvent = new Event.Models.Event
                {
                    Event_Id = eventId,
                    Organizer_Id = organizerId,
                    Venue_Id = venueId,
                    Event_Type = def.EventType,
                    Title = def.Title,
                    Category = def.Category,
                    Age_Category = def.AgeCategory,
                    Description_Url = descriptionFilePath,
                    Image_Url = def.ImageUrl,
                    Date_Time = DateTime.SpecifyKind(DateTime.Parse(def.DateTime), DateTimeKind.Utc),
                    Duration_Hours = (decimal)def.DurationHours,
                    Status = "Live",
                    Requires_Staff = def.EventType != "Virtual",
                    Virtual_Url = def.EventType != "Physical" ? "https://meet.jit.si/event-" + eventId : null,
                    Virtual_Password_Hash = def.EventType != "Physical" ? "61eJPjKCSth/N5T72cAbXmAqrhLOyHLRuTnmKG1jGO3eu/Fwp2nldfpJ9UOW4S3p" : null
                };

                seededEvents.Add(newEvent);

                // Ticket Tiers
                ticketTiersToSeed.Add(new EventTicketTier
                {
                    Event_Id = eventId,
                    Tier_Name = "General Admission",
                    Price = (decimal)def.MinPrice,
                    Tickets_Sold = 0
                });

                if (def.EventType != "Virtual")
                {
                    ticketTiersToSeed.Add(new EventTicketTier
                    {
                        Event_Id = eventId,
                        Tier_Name = "VIP Access",
                        Price = (decimal)(def.MinPrice * 2.5),
                        Tickets_Sold = 0
                    });
                }
            }

            await context.Events.AddRangeAsync(seededEvents);
            await context.EventTicketTiers.AddRangeAsync(ticketTiersToSeed);
            await context.SaveChangesAsync();
            #endregion

            #region Seed Staff Allocations (Table: EventStaffAllocations)
            // =========================================================================
            // 10. SEED STAFF ALLOCATIONS (Table: "EventStaffAllocations")
            // Allocate staff region-wise for physical events. Set IsAllocated = true only for them.
            // =========================================================================
            var staffAllocations = new List<EventStaffAllocation>();
            foreach (var ev in seededEvents)
            {
                if (!ev.Requires_Staff || !ev.Venue_Id.HasValue) continue;

                var venue = venues.First(v => v.Venue_Id == ev.Venue_Id.Value);
                // 1 staff per 100 seats -> 550 total seats -> 6 staffs
                int requiredStaff = 6;

                var regionStaffs = staffs
                    .Where(s => s.Region_Id == venue.Region_Id && !s.IsAllocated)
                    .Take(requiredStaff)
                    .ToList();

                // Fallback in case we ran out of unallocated staff in the region
                if (regionStaffs.Count < requiredStaff)
                {
                    var extraStaffs = staffs
                        .Where(s => s.Region_Id == venue.Region_Id && !regionStaffs.Contains(s))
                        .Take(requiredStaff - regionStaffs.Count)
                        .ToList();
                    regionStaffs.AddRange(extraStaffs);
                }

                foreach (var staff in regionStaffs)
                {
                    staffAllocations.Add(new EventStaffAllocation
                    {
                        Event_Id = ev.Event_Id,
                        Employee_ID = staff.Employee_ID
                    });
                    staff.IsAllocated = true; // Mark as allocated!
                }
            }
            context.Staffs.UpdateRange(staffs); // Persist IsAllocated = true changes
            await context.EventStaffAllocations.AddRangeAsync(staffAllocations);
            await context.SaveChangesAsync();
            #endregion

            #region Seed Bookings and Transactions (Tables: Bookings, BookingDetails, BookingPayments, Transactions)
            // =========================================================================
            // 11. SEED BOOKINGS, PAYMENTS & LEDGER TRANSACTIONS
            // Rank top 4 events:
            // 1. 10005 (AR Rahman Live Symphony): 25 Bookings
            // 2. 10002 (Chennai Tech Expo): 20 Bookings
            // 3. 10018 (Madurai Chithirai Art Fest): 15 Bookings
            // 4. 10008 (IIT Madras Hackathon): 10 Bookings
            // Other events: 0 to 2 bookings.
            // =========================================================================
            int bookingIdCounter = 10001;
            long transactionIdCounter = 5000000000000001;

            var rankingTargetEvents = new Dictionary<int, int>
            {
                { 10005, 25 },
                { 10002, 20 },
                { 10018, 15 },
                { 10008, 10 }
            };

            // Seed other random events with some bookings for visual excellence
            var otherEventsWithBookings = new int[] { 10001, 10003, 10009, 10010, 10015, 10020, 10025, 10031, 10034, 10037, 10043, 10046 };
            foreach (var evtId in otherEventsWithBookings)
            {
                if (!rankingTargetEvents.ContainsKey(evtId))
                {
                    rankingTargetEvents.Add(evtId, new Random().Next(1, 3));
                }
            }

            var bookingsList = new List<Booking>();
            var bookingDetailsList = new List<BookingDetail>();
            var bookingPaymentsList = new List<BookingPayment>();
            var transactionsList = new List<Transaction>();

            foreach (var kvp in rankingTargetEvents)
            {
                int eventId = kvp.Key;
                int count = kvp.Value;

                var ev = seededEvents.First(e => e.Event_Id == eventId);
                var tiers = ticketTiersToSeed.Where(t => t.Event_Id == eventId).ToList();

                for (int b = 0; b < count; b++)
                {
                    int bookingId = bookingIdCounter++;
                    long transactionId = transactionIdCounter++;

                    // Choose attendee (User IDs 10003 to 10030) - avoid organizers
                    int attendeeId = 10003 + (b % 28);

                    // Choose ticket tier & quantity
                    var selectedTier = tiers[b % tiers.Count];
                    int quantity = (b % 3) + 1; // 1, 2 or 3 tickets

                    decimal ticketPrice = selectedTier.Price;
                    decimal faceValue = ticketPrice * quantity;
                    decimal convFee = settings.Ticket_Fixed_Fee * quantity;
                    decimal totalAmount = faceValue + convFee;
                    decimal commissionCut = (faceValue * (settings.Ticket_Commission_Percentage / 100m)) + convFee;

                    // Increment tickets sold count
                    selectedTier.Tickets_Sold += quantity;

                    // Booking Path QR Code
                    string qrRelativePath = $"users/{attendeeId}/bookings/qr_{bookingId}.png";
                    string qrUrl = await storageService.SaveTextAsync(qrRelativePath, $"Booking ID: {bookingId}, Event ID: {eventId}, Attendee ID: {attendeeId}");

                    bookingsList.Add(new Booking
                    {
                        Booking_Id = bookingId,
                        Attendee_Id = attendeeId,
                        Event_Id = eventId,
                        Booking_Status = "Confirmed",
                        Qr_Code_Path = qrUrl,
                        Qr_Secret_Hash = "sec_hash_" + Guid.NewGuid().ToString("N").Substring(0, 16),
                        CheckIn_Status = b % 5 == 0 ? "CheckedIn" : "Pending",
                        Created_At = DateTime.UtcNow.AddDays(-b),
                        Virtual_Url = ev.Event_Type != "Physical" ? "https://meet.jit.si/event-" + eventId : null
                    });

                    bookingDetailsList.Add(new BookingDetail
                    {
                        Booking_Id = bookingId,
                        Tier_Name = selectedTier.Tier_Name,
                        Quantity = quantity
                    });

                    transactionsList.Add(new Transaction
                    {
                        Transaction_Id = transactionId,
                        Sender_Id = $"Attendee_User_{attendeeId}",
                        Receiver_Id = "Platform_Escrow",
                        Transaction_Type = "BookingPayment",
                        Related_Id = bookingId,
                        Amount = totalAmount,
                        Currency = "INR",
                        Payment_Method_Details = "card",
                        Status = "Success",
                        Refunded_Amount = 0m,
                        Remarks = $"Ticket purchase for Event: {ev.Title}",
                        Transaction_Reference = "ch_booking_" + Guid.NewGuid().ToString("N").Substring(0, 12),
                        Created_At = DateTime.UtcNow.AddDays(-b)
                    });

                    bookingPaymentsList.Add(new BookingPayment
                    {
                        Booking_Payment_Id = bookingId, // Use same ID or auto-generated
                        Booking_Id = bookingId,
                        Transaction_Id = transactionId,
                        Amount = totalAmount,
                        Platform_Fee_Cut = commissionCut,
                        Payment_Status = "Success",
                        Created_At = DateTime.UtcNow.AddDays(-b)
                    });
                }
            }

            context.EventTicketTiers.UpdateRange(ticketTiersToSeed); // save tickets sold counts
            await context.Bookings.AddRangeAsync(bookingsList);
            await context.BookingDetails.AddRangeAsync(bookingDetailsList);
            await context.Transactions.AddRangeAsync(transactionsList);
            await context.BookingPayments.AddRangeAsync(bookingPaymentsList);
            await context.SaveChangesAsync();
            #endregion

            #region Seed Waitlists
            // =========================================================================
            // 11. SEED WAITLISTS
            // =========================================================================
            var waitlists = new List<Waitlist>
            {
                new Waitlist { Waitlist_Id = 10001, Event_Id = 10001, Attendee_Id = 10010, Tier_Name = "VIP", Quantity = 2, Status = "Waiting", Position = 1, Joined_At = DateTime.UtcNow.AddDays(-2) },
                new Waitlist { Waitlist_Id = 10002, Event_Id = 10001, Attendee_Id = 10011, Tier_Name = "VIP", Quantity = 1, Status = "Waiting", Position = 2, Joined_At = DateTime.UtcNow.AddDays(-1) },
                new Waitlist { Waitlist_Id = 10003, Event_Id = 10008, Attendee_Id = 10012, Tier_Name = "VIP Access", Quantity = 1, Status = "Notified", Position = 0, Joined_At = DateTime.UtcNow.AddDays(-3), Notified_At = DateTime.UtcNow.AddMinutes(-30), Expires_At = DateTime.UtcNow.AddHours(2) }
            };
            await context.Waitlists.AddRangeAsync(waitlists);
            await context.SaveChangesAsync();
            #endregion

            #region Seed Organizer Upfront Payments (Table: OrganizerUpfrontPayments)
            // =========================================================================
            // 12. SEED ORGANIZER UPFRONT PAYMENTS
            // For all 48 events, organizers paid upfront fees
            // =========================================================================
            var upfrontPayments = new List<OrganizerUpfrontPayment>();
            var newTransactions = new List<Transaction>();
            foreach (var ev in seededEvents)
            {
                long transactionId = transactionIdCounter++;

                decimal upfrontFee = 0;
                if (ev.Event_Type == "Virtual")
                {
                    upfrontFee = settings.Virtual_Event_Activation_Fee;
                }
                else
                {
                    var venue = venues.First(v => v.Venue_Id == ev.Venue_Id!.Value);
                    decimal venueCost = venue.Hourly_Price * ev.Duration_Hours;
                    upfrontFee = settings.Physical_Event_Activation_Fee + venueCost;

                    if (ev.Requires_Staff)
                    {
                        int allocatedCount = staffAllocations.Count(sa => sa.Event_Id == ev.Event_Id);
                        upfrontFee += settings.Staff_Flat_Rate * allocatedCount;
                    }
                }

                newTransactions.Add(new Transaction
                {
                    Transaction_Id = transactionId,
                    Sender_Id = $"Organizer_User_{ev.Organizer_Id}",
                    Receiver_Id = "Platform_Escrow",
                    Transaction_Type = "OrganizerUpfrontPayment",
                    Related_Id = ev.Event_Id,
                    Amount = upfrontFee,
                    Currency = "INR",
                    Payment_Method_Details = "card",
                    Status = "Success",
                    Refunded_Amount = 0m,
                    Remarks = $"Upfront activation fee for Event: {ev.Title}",
                    Transaction_Reference = "ch_upfront_" + Guid.NewGuid().ToString("N").Substring(0, 12),
                    Created_At = DateTime.UtcNow.AddDays(-15)
                });

                upfrontPayments.Add(new OrganizerUpfrontPayment
                {
                    Upfront_Payment_Id = ev.Event_Id, // unique per event mapping
                    Event_Id = ev.Event_Id,
                    Transaction_Id = transactionId,
                    Amount = upfrontFee,
                    Payment_Status = "Success",
                    Created_At = DateTime.UtcNow.AddDays(-15)
                });
            }
            await context.Transactions.AddRangeAsync(newTransactions);
            await context.OrganizerUpfrontPayments.AddRangeAsync(upfrontPayments);
            await context.SaveChangesAsync();
            #endregion

            #region Seed Support Tickets, Feedbacks, and Reports (Tables: SupportTickets, EventFeedbacks, EventReports)
            // =========================================================================
            // 13. SEED EXTRA USER-SIDE TABLES (SupportTickets, Feedbacks, Reports)
            // =========================================================================
            var supportTickets = new List<SupportTicket>
            {
                new SupportTicket { Ticket_Id = 10001, User_Id = 10003, ConcernUrl = "/assets/users/10003/support/ticket_10001.json", RequestType = "REF", Status = "Open", EsclationStatus = "Available", RelatedId = 10001 },
                new SupportTicket { Ticket_Id = 10002, User_Id = 10004, ConcernUrl = "/assets/users/10004/support/ticket_10002.json", RequestType = "GEN", Status = "Closed", EsclationStatus = "Unavailable" },
                new SupportTicket { Ticket_Id = 10003, User_Id = 10005, ConcernUrl = "/assets/users/10005/support/ticket_10003.json", RequestType = "GEN", Status = "Open", EsclationStatus = "Unavailable" },
                new SupportTicket { Ticket_Id = 10004, User_Id = 10006, ConcernUrl = "/assets/users/10006/support/ticket_10004.json", RequestType = "REF", Status = "Open", EsclationStatus = "Available", RelatedId = 10002 },
                new SupportTicket { Ticket_Id = 10005, User_Id = 10007, ConcernUrl = "/assets/users/10007/support/ticket_10005.json", RequestType = "GEN", Status = "Closed", EsclationStatus = "Unavailable" },
                new SupportTicket { Ticket_Id = 10006, User_Id = 10008, ConcernUrl = "/assets/users/10008/support/ticket_10006.json", RequestType = "REF", Status = "Open", EsclationStatus = "Available", RelatedId = 10003 },
                new SupportTicket { Ticket_Id = 10007, User_Id = 10009, ConcernUrl = "/assets/users/10009/support/ticket_10007.json", RequestType = "GEN", Status = "Open", EsclationStatus = "Unavailable" },
                new SupportTicket { Ticket_Id = 10008, User_Id = 10010, ConcernUrl = "/assets/users/10010/support/ticket_10008.json", RequestType = "GEN", Status = "Closed", EsclationStatus = "Unavailable" },
                new SupportTicket { Ticket_Id = 10009, User_Id = 10011, ConcernUrl = "/assets/users/10011/support/ticket_10009.json", RequestType = "REF", Status = "Open", EsclationStatus = "Available", RelatedId = 10004 },
                new SupportTicket { Ticket_Id = 10010, User_Id = 10012, ConcernUrl = "/assets/users/10012/support/ticket_10010.json", RequestType = "GEN", Status = "Open", EsclationStatus = "Unavailable" }
            };

            var supportTicketSeedData = new[]
            {
                new { TicketId = 10001, UserId = 10003, Subject = "Refund Request for Booking #10001", Message = "I would like to request a refund for my booking due to scheduling conflicts.", Response = (string?)null },
                new { TicketId = 10002, UserId = 10004, Subject = "Ticket delivery query", Message = "Can I show my QR code ticket on my mobile phone at entrance?", Response = "Yes, mobile QR codes are fully accepted." },
                new { TicketId = 10003, UserId = 10005, Subject = "Event timing change", Message = "The event start time seems to have changed; could you confirm the new schedule?", Response = (string?)null },
                new { TicketId = 10004, UserId = 10006, Subject = "Venue access issue", Message = "I need help locating the venue entrance for the event tomorrow.", Response = "The entrance details are available in your confirmation email." },
                new { TicketId = 10005, UserId = 10007, Subject = "Booking confirmation problem", Message = "My payment was successful but the booking confirmation did not arrive.", Response = "We have re-sent your confirmation to the registered email." },
                new { TicketId = 10006, UserId = 10008, Subject = "Refund delay", Message = "My refund request has been pending for several days and I need an update.", Response = "The refund is currently being processed and should reflect within 3-5 business days." },
                new { TicketId = 10007, UserId = 10009, Subject = "Attendee list issue", Message = "I need to update the attendee list for my group booking.", Response = "Please contact the organizer directly for attendee changes." },
                new { TicketId = 10008, UserId = 10010, Subject = "App login issue", Message = "I cannot log into the app after resetting my password.", Response = "Please try password recovery again or reset your password using the login page." },
                new { TicketId = 10009, UserId = 10011, Subject = "Event reminder problem", Message = "I am still not receiving reminder emails for upcoming events.", Response = "Reminder settings have been updated for your account." },
                new { TicketId = 10010, UserId = 10012, Subject = "Voucher code not working", Message = "The promotional voucher code is being rejected during checkout.", Response = "The voucher code has been verified and should work now." }
            };

            foreach (var seed in supportTicketSeedData)
            {
                string ticketFile = $"users/{seed.UserId}/support/ticket_{seed.TicketId}.json";
                await storageService.SaveTextAsync(ticketFile, JsonSerializer.Serialize(new { Subject = seed.Subject, Message = seed.Message, Response = seed.Response }, new JsonSerializerOptions { WriteIndented = true }));
            }

            await context.SupportTickets.AddRangeAsync(supportTickets);

            // A few Event Feedbacks
            var feedbacks = new List<EventFeedback>
            {
                new EventFeedback { Feedback_Id = 10001, Event_Id = 10005, Attendee_Id = 10003, Rating = 5, Review = "AR Rahman Live was absolutely outstanding! Unbelievable orchestrations." },
                new EventFeedback { Feedback_Id = 10002, Event_Id = 10002, Attendee_Id = 10004, Rating = 4, Review = "Good panels on Generative AI and tech scaling. Learned a lot." }
            };
            await context.EventFeedbacks.AddRangeAsync(feedbacks);

            var reports = new List<EventReport>
            {
                new EventReport { Report_Id = 10001, Event_Id = 10033, Reporter_Id = 10005, ReportUrl = "/assets/users/10005/reports/report_10001.json", ResponseAction = "Dismissed", Created_At = DateTime.UtcNow.AddDays(-2) },
                new EventReport { Report_Id = 10002, Event_Id = 10035, Reporter_Id = 10006, ReportUrl = "/assets/users/10006/reports/report_10002.json", ResponseAction = "Pending", Created_At = DateTime.UtcNow.AddDays(-1) },
                new EventReport { Report_Id = 10003, Event_Id = 10037, Reporter_Id = 10007, ReportUrl = "/assets/users/10007/reports/report_10003.json", ResponseAction = "Under Review", Created_At = DateTime.UtcNow.AddDays(-3) },
                new EventReport { Report_Id = 10004, Event_Id = 10039, Reporter_Id = 10008, ReportUrl = "/assets/users/10008/reports/report_10004.json", ResponseAction = "Dismissed", Created_At = DateTime.UtcNow.AddDays(-4) },
                new EventReport { Report_Id = 10005, Event_Id = 10041, Reporter_Id = 10009, ReportUrl = "/assets/users/10009/reports/report_10005.json", ResponseAction = "Resolved", Created_At = DateTime.UtcNow.AddDays(-5) }
            };

            var reportSeedData = new[]
            {
                new { ReportId = 10001, ReporterId = 10005, Reason = "Event venue was dangerously crowded with insufficient entry management." },
                new { ReportId = 10002, ReporterId = 10006, Reason = "The organizer did not provide proper safety instructions for attendees." },
                new { ReportId = 10003, ReporterId = 10007, Reason = "There were repeated audio issues during the keynote session." },
                new { ReportId = 10004, ReporterId = 10008, Reason = "The event page displayed incorrect venue details." },
                new { ReportId = 10005, ReporterId = 10009, Reason = "Unapproved commercial promotions were shared in the event chat." }
            };

            foreach (var reportSeed in reportSeedData)
            {
                string reportFile = $"users/{reportSeed.ReporterId}/reports/report_{reportSeed.ReportId}.json";
                await storageService.SaveTextAsync(reportFile, JsonSerializer.Serialize(new { Reason = reportSeed.Reason }, new JsonSerializerOptions { WriteIndented = true }));
            }

            await context.EventReports.AddRangeAsync(reports);
            await context.SaveChangesAsync();
            #endregion

            #region Seed Notifications (Table: Notifications)
            // =========================================================================
            // 14. SEED NOTIFICATIONS (Table: "Notifications")
            // =========================================================================
            var seedNotifications = new List<Notification>
            {
                new Notification { Notification_Id = 10001, Recipient_Email = "amit.sharma@example.com", MessageUrl = "/assets/notifications/10001.json", Status = "Sent", Retry_Count = 0, Created_At = DateTime.UtcNow.AddDays(-5), Sent_At = DateTime.UtcNow.AddDays(-5) },
                new Notification { Notification_Id = 10002, Recipient_Email = "priya.patel@example.com", MessageUrl = "/assets/notifications/10002.json", Status = "Sent", Retry_Count = 0, Created_At = DateTime.UtcNow.AddDays(-4), Sent_At = DateTime.UtcNow.AddDays(-4) },
                new Notification { Notification_Id = 10003, Recipient_Email = "rajesh.kumar@example.com", MessageUrl = "/assets/notifications/10003.json", Status = "Sent", Retry_Count = 0, Created_At = DateTime.UtcNow.AddDays(-3), Sent_At = DateTime.UtcNow.AddDays(-3) },
                new Notification { Notification_Id = 10004, Recipient_Email = "anjali.singh@example.com", MessageUrl = "/assets/notifications/10004.json", Status = "Sent", Retry_Count = 0, Created_At = DateTime.UtcNow.AddDays(-2), Sent_At = DateTime.UtcNow.AddDays(-2) },
                new Notification { Notification_Id = 10005, Recipient_Email = "rohan.nair@example.com", MessageUrl = "/assets/notifications/10005.json", Status = "Sent", Retry_Count = 0, Created_At = DateTime.UtcNow.AddDays(-1), Sent_At = DateTime.UtcNow.AddDays(-1) }
            };


            var notificationData = new[]
            {
                new { Id = 10001, Subject = "Booking Confirmed - Margazhi Music Festival 2026", Body = "Dear Amit, Your booking for Margazhi Music Festival 2026 is confirmed. Ticket QR code has been generated." },
                new { Id = 10002, Subject = "Event Registration - Chennai Tech Expo", Body = "Dear Priya, You have successfully registered for the Chennai Tech Expo & AI Summit." },
                new { Id = 10003, Subject = "Food Carnival Ticket - Tamil Nadu Traditional Food", Body = "Dear Rajesh, Your ticket for the Traditional Food Carnival is ready." },
                new { Id = 10004, Subject = "Film Festival Pass - Chennai International Film Festival", Body = "Dear Anjali, Your passes for the International Film Festival have been confirmed." },
                new { Id = 10005, Subject = "Concert Ticket - A.R. Rahman Live Symphony", Body = "Dear Rohan, Your VIP ticket for A.R. Rahman Live Symphony Concert is confirmed." }
            };

            foreach (var nData in notificationData)
            {
                string jsonFilePath = $"notifications/{nData.Id}.json";
                var payload = new
                {
                    Subject = nData.Subject,
                    Body = nData.Body
                };
                string jsonContent = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
                await storageService.SaveTextAsync(jsonFilePath, jsonContent);
            }

            await context.Notifications.AddRangeAsync(seedNotifications);
            await context.SaveChangesAsync();
            #endregion
        }

        #region Helper Functions

        private static async Task<string> CreateDescriptionFile(IFileStorageService storageService, int eventId, string descriptionText)
        {
            string relativePath = $"events/{eventId}/description.md";
            return await storageService.SaveTextAsync(relativePath, descriptionText);
        }

        private class EventSeedDef
        {
            public int EventId { get; set; }
            public string Title { get; set; } = "";
            public string EventType { get; set; } = "";
            public string DateTime { get; set; } = "";
            public double DurationHours { get; set; }
            public string VenueName { get; set; } = "";
            public string RegionId { get; set; } = "";
            public string Category { get; set; } = "";
            public string AgeCategory { get; set; } = "";
            public double MinPrice { get; set; }
            public string ImageUrl { get; set; } = "";
            public string Description { get; set; } = "";
        }

        private static List<EventSeedDef> GetEventDefinitions()
        {
            return new List<EventSeedDef>
            {
                // Chennai
                new EventSeedDef { EventId = 10001, Title = "Margazhi Music Festival 2026", EventType = "Physical", DateTime = "2026-12-15T18:00:00Z", DurationHours = 4, VenueName = "Music Academy Hall", RegionId = "REG01", Category = "Music", AgeCategory = "ALL", MinPrice = 250.00, ImageUrl = "https://images.unsplash.com/photo-1514525253161-7a46d19cd819?w=500&auto=format&fit=crop&q=80", Description = "Experience the rich heritage of classical Carnatic music performances by legendary vocalists." },
                new EventSeedDef { EventId = 10002, Title = "Chennai Tech Expo & AI Summit", EventType = "Hybrid", DateTime = "2026-07-20T09:00:00Z", DurationHours = 8, VenueName = "Chennai Trade Centre", RegionId = "REG01", Category = "Technology", AgeCategory = "ALL", MinPrice = 150.00, ImageUrl = "https://images.unsplash.com/photo-1540575467063-178a50c2df87?w=500&auto=format&fit=crop&q=80", Description = "Discover recent innovations in Generative AI, cloud computing, and cybersecurity." },
                new EventSeedDef { EventId = 10003, Title = "Tamil Nadu Traditional Food Carnival", EventType = "Physical", DateTime = "2026-08-05T11:00:00Z", DurationHours = 10, VenueName = "Island Grounds", RegionId = "REG01", Category = "Food & Drinks", AgeCategory = "ALL", MinPrice = 100.00, ImageUrl = "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=500&auto=format&fit=crop&q=80", Description = "Relish local delicacies from Madurai, Chettinad, and Nellai alongside cultural folk arts." },
                new EventSeedDef { EventId = 10004, Title = "Chennai International Film Festival", EventType = "Physical", DateTime = "2026-09-10T10:00:00Z", DurationHours = 48, VenueName = "Sathyam Cinemas Complex", RegionId = "REG01", Category = "Arts", AgeCategory = "ALL", MinPrice = 500.00, ImageUrl = "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=500&auto=format&fit=crop&q=80", Description = "Screening critically acclaimed movies and documentaries from world cinema directors." },
                new EventSeedDef { EventId = 10005, Title = "A.R. Rahman Live Symphony Concert", EventType = "Physical", DateTime = "2026-10-18T19:00:00Z", DurationHours = 3.5, VenueName = "YMCA Grounds Nandanam", RegionId = "REG01", Category = "Music", AgeCategory = "ADL", MinPrice = 999.00, ImageUrl = "https://images.unsplash.com/photo-1470225620780-dba8ba36b745?w=1200&auto=format&fit=crop&q=80", Description = "An epic orchestral concert rendering the magical soundtracks of India's Mozart A.R. Rahman." },
                new EventSeedDef { EventId = 10006, Title = "Chennai Standup Comedy & Art Night", EventType = "Physical", DateTime = "2026-11-20T19:00:00Z", DurationHours = 2.5, VenueName = "Kamarajar Memorial Hall", RegionId = "REG01", Category = "Arts", AgeCategory = "ALL", MinPrice = 499.00, ImageUrl = "https://images.unsplash.com/photo-1527224857830-43a7acc85260?w=500&auto=format&fit=crop&q=80", Description = "A delightful exhibition of local standup comedy and theatrical arts performing live in Chennai." },
                new EventSeedDef { EventId = 10007, Title = "East Coast Road Surfing & Heritage Fest", EventType = "Physical", DateTime = "2026-10-02T06:00:00Z", DurationHours = 12, VenueName = "Kovalam Beach Point", RegionId = "REG01", Category = "Arts", AgeCategory = "ALL", MinPrice = 299.00, ImageUrl = "https://images.unsplash.com/photo-1502680390469-be75c86b636f?w=500&auto=format&fit=crop&q=80", Description = "A beachside heritage exhibition featuring traditional water sports, surfing workshops, and food stalls." },
                new EventSeedDef { EventId = 10008, Title = "IIT Madras Tech & AI Hackathon", EventType = "Physical", DateTime = "2026-09-05T08:00:00Z", DurationHours = 36, VenueName = "IITM ICSR Auditorium", RegionId = "REG01", Category = "Technology", AgeCategory = "ALL", MinPrice = 99.00, ImageUrl = "https://images.unsplash.com/photo-1517694712202-14dd9538aa97?w=500&auto=format&fit=crop&q=80", Description = "A premier technology hackathon focusing on generative AI and deep tech software applications." },
                
                // Coimbatore
                new EventSeedDef { EventId = 10009, Title = "Coimbatore SaaS & DeepTech Summit", EventType = "Physical", DateTime = "2026-08-25T09:30:00Z", DurationHours = 6.5, VenueName = "CODISSIA Hall A", RegionId = "REG02", Category = "Technology", AgeCategory = "ALL", MinPrice = 350.00, ImageUrl = "https://images.unsplash.com/photo-1504384308090-c894fdcc538d?w=500&auto=format&fit=crop&q=80", Description = "Bringing together top tech founders and venture capitals to discuss SaaS scalability." },
                new EventSeedDef { EventId = 10010, Title = "Western Ghats Organic Food Fest", EventType = "Physical", DateTime = "2026-09-05T10:00:00Z", DurationHours = 8, VenueName = "VOC Park Grounds", RegionId = "REG02", Category = "Food & Drinks", AgeCategory = "ALL", MinPrice = 50.00, ImageUrl = "https://images.unsplash.com/photo-1498837167922-ddd27525d352?w=500&auto=format&fit=crop&q=80", Description = "Taste millets, honey, and organic produce sourced directly from western ghats farming cooperatives." },
                new EventSeedDef { EventId = 10011, Title = "Coimbatore Business Leadership Meet", EventType = "Hybrid", DateTime = "2026-10-12T14:00:00Z", DurationHours = 4, VenueName = "Suntec Exhibition Centre", RegionId = "REG02", Category = "Business", AgeCategory = "ALL", MinPrice = 1500.00, ImageUrl = "https://images.unsplash.com/photo-1590283603385-17ffb3a7f29f?w=500&auto=format&fit=crop&q=80", Description = "Corporate strategy discussions focusing on manufacturing automation and smart textile mills." },
                new EventSeedDef { EventId = 10012, Title = "Kovai Handloom & Khadi Exhibition", EventType = "Physical", DateTime = "2026-11-01T10:00:00Z", DurationHours = 12, VenueName = "Gandhipuram Cultural Center", RegionId = "REG02", Category = "Arts", AgeCategory = "ALL", MinPrice = 50.00, ImageUrl = "https://images.unsplash.com/photo-1460661419201-fd4cecdf8a8b?w=500&auto=format&fit=crop&q=80", Description = "Promoting local weavers and displaying hand-woven sarees, kurtas, and traditional apparel." },
                new EventSeedDef { EventId = 10013, Title = "Kovai Carnatic Vocal & Veena Recital", EventType = "Physical", DateTime = "2026-12-20T17:30:00Z", DurationHours = 3, VenueName = "Coimbatore Kalaiarangam", RegionId = "REG02", Category = "Music", AgeCategory = "ALL", MinPrice = 150.00, ImageUrl = "https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=500&auto=format&fit=crop&q=80", Description = "An exquisite classical music concert featuring Carnatic vocal and veena recitals by local artists." },
                new EventSeedDef { EventId = 10014, Title = "Nilgiri Tea & Organic Coffee Tasting", EventType = "Physical", DateTime = "2026-10-15T11:00:00Z", DurationHours = 4, VenueName = "Coimbatore Botanical Club", RegionId = "REG02", Category = "Food & Drinks", AgeCategory = "ALL", MinPrice = 300.00, ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=500&auto=format&fit=crop&q=80", Description = "A unique food tasting workshop showcasing organic tea and coffee varieties from Nilgiris hills." },

                // Madurai
                new EventSeedDef { EventId = 10015, Title = "Madurai Jigarthanda & Food Fest", EventType = "Physical", DateTime = "2026-08-12T12:00:00Z", DurationHours = 9, VenueName = "Tamukkam Grounds", RegionId = "REG03", Category = "Food & Drinks", AgeCategory = "ALL", MinPrice = 75.00, ImageUrl = "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=500&auto=format&fit=crop&q=80", Description = "Explore the culinary secrets of the temple city, featuring Jigarthanda, Kari Dosai, and Bun Parotta." },
                new EventSeedDef { EventId = 10016, Title = "Heritage Temple Art & Sculpting Seminar", EventType = "Physical", DateTime = "2026-09-08T15:00:00Z", DurationHours = 3, VenueName = "Meenakshi Temple Convention Hall", RegionId = "REG03", Category = "Arts", AgeCategory = "ALL", MinPrice = 150.00, ImageUrl = "https://images.unsplash.com/photo-1513364776144-60967b0f800f?w=500&auto=format&fit=crop&q=80", Description = "Learn about Dravidian temple architecture, stone carvings, and ancient sculpting techniques." },
                new EventSeedDef { EventId = 10017, Title = "Madurai Startup & Innovation Pitch", EventType = "Virtual", DateTime = "2026-10-05T10:00:00Z", DurationHours = 5, VenueName = "", RegionId = "REG03", Category = "Business", AgeCategory = "ALL", MinPrice = 199.00, ImageUrl = "https://images.unsplash.com/photo-1515187029135-18ee286d815b?w=500&auto=format&fit=crop&q=80", Description = "A virtual pitch deck platform connecting local tier-2 city startup founders with active angel networks." },
                new EventSeedDef { EventId = 10018, Title = "Madurai Chithirai Heritage Art Festival", EventType = "Physical", DateTime = "2026-11-05T16:00:00Z", DurationHours = 6, VenueName = "Madurai Palace Grounds", RegionId = "REG03", Category = "Arts", AgeCategory = "ALL", MinPrice = 120.00, ImageUrl = "https://images.unsplash.com/photo-1561089689-6ec5e37b463b?w=500&auto=format&fit=crop&q=80", Description = "A spectacular exhibition of traditional Tamil folk arts, street theater, and ancient temple culture." },
                new EventSeedDef { EventId = 10019, Title = "Madurai Food & Jigarthanda Carnival", EventType = "Physical", DateTime = "2026-08-15T13:00:00Z", DurationHours = 8, VenueName = "K.K. Nagar Exhibition Ground", RegionId = "REG03", Category = "Food & Drinks", AgeCategory = "ALL", MinPrice = 50.00, ImageUrl = "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=500&auto=format&fit=crop&q=80", Description = "A delicious food carnival celebrating the local culinary heritage and sweet jigarthanda of Madurai." },

                // Trichy
                new EventSeedDef { EventId = 10020, Title = "Rockfort Business Conclave 2026", EventType = "Physical", DateTime = "2026-09-15T09:00:00Z", DurationHours = 7, VenueName = "Sangam Hotel Conference Hall", RegionId = "REG04", Category = "Business", AgeCategory = "ALL", MinPrice = 800.00, ImageUrl = "https://images.unsplash.com/photo-1517245386807-bb43f82c33c4?w=500&auto=format&fit=crop&q=80", Description = "Interactive panels focusing on manufacturing supply chains and industrial trade expansion." },
                new EventSeedDef { EventId = 10021, Title = "Trichy AgriTech & Precision Farming Expo", EventType = "Physical", DateTime = "2026-10-22T10:00:00Z", DurationHours = 8, VenueName = "National College Grounds", RegionId = "REG04", Category = "Technology", AgeCategory = "ALL", MinPrice = 50.00, ImageUrl = "https://images.unsplash.com/photo-1530595467537-0b5996c41f2d?w=500&auto=format&fit=crop&q=80", Description = "Showcasing drone spraying, IoT soil sensors, and high-yielding organic hybrid crop practices." },
                new EventSeedDef { EventId = 10022, Title = "Kaveri Musical Concert", EventType = "Physical", DateTime = "2026-11-15T18:30:00Z", DurationHours = 4, VenueName = "Trichy Arts Auditorium", RegionId = "REG04", Category = "Music", AgeCategory = "ALL", MinPrice = 200.00, ImageUrl = "https://images.unsplash.com/photo-1465847899084-d164df4dedc6?w=1200&auto=format&fit=crop&q=80", Description = "Classical and cinematic music medleys performed by renowned playback singers on the banks of Kaveri." },
                new EventSeedDef { EventId = 10023, Title = "Trichy Heritage Walk & Rockfort Tour", EventType = "Physical", DateTime = "2026-10-10T07:00:00Z", DurationHours = 3.5, VenueName = "Rockfort Temple Steps Entrance", RegionId = "REG04", Category = "Arts", AgeCategory = "ALL", MinPrice = 100.00, ImageUrl = "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?w=500&auto=format&fit=crop&q=80", Description = "A historic heritage walk discovering the ancient stone art and dynastic architecture of Rockfort Trichy." },
                new EventSeedDef { EventId = 10024, Title = "Trichy Tech Startup & SaaS Growth Summit", EventType = "Hybrid", DateTime = "2026-12-05T09:30:00Z", DurationHours = 8, VenueName = "NIT Trichy Convention Hall", RegionId = "REG04", Category = "Business", AgeCategory = "ALL", MinPrice = 450.00, ImageUrl = "https://images.unsplash.com/photo-1522071820081-009f0129c71c?w=500&auto=format&fit=crop&q=80", Description = "A tech business summit for startup scaling, SaaS, and IT innovation in tier-2 cities of Tamil Nadu." },

                // Mumbai
                new EventSeedDef { EventId = 10025, Title = "Mumbai Bollywood Fusion Night", EventType = "Physical", DateTime = "2026-08-10T19:00:00Z", DurationHours = 4, VenueName = "Jio World Convention Centre", RegionId = "REG05", Category = "Music", AgeCategory = "ALL", MinPrice = 1200.00, ImageUrl = "https://images.unsplash.com/photo-1470225620780-dba8ba36b745?w=500&auto=format&fit=crop&q=80", Description = "Experience a grand night of live Bollywood musical medleys and spectacular dance performances." },
                new EventSeedDef { EventId = 10026, Title = "Mumbai International Film Festival (MIFF)", EventType = "Physical", DateTime = "2026-09-05T10:00:00Z", DurationHours = 6, VenueName = "NCPA Nariman Point", RegionId = "REG05", Category = "Arts", AgeCategory = "ALL", MinPrice = 350.00, ImageUrl = "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=500&auto=format&fit=crop&q=80", Description = "Screening contemporary regional cinema and foreign films with filmmaker discussions." },
                new EventSeedDef { EventId = 10027, Title = "Gateway of India Heritage Walk", EventType = "Physical", DateTime = "2026-10-12T07:00:00Z", DurationHours = 3, VenueName = "Gateway Entrance Plaza", RegionId = "REG05", Category = "Arts", AgeCategory = "ALL", MinPrice = 199.00, ImageUrl = "https://images.unsplash.com/photo-1570168007204-dfb528c6958f?w=500&auto=format&fit=crop&q=80", Description = "A guided historical tour highlighting South Mumbai's colonial history and architecture." },

                // Delhi
                new EventSeedDef { EventId = 10028, Title = "Delhi Food & Kebab Fest", EventType = "Physical", DateTime = "2026-09-18T12:00:00Z", DurationHours = 8, VenueName = "Jawaharlal Nehru Stadium", RegionId = "REG06", Category = "Food & Drinks", AgeCategory = "ALL", MinPrice = 150.00, ImageUrl = "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=500&auto=format&fit=crop&q=80", Description = "Taste the legendary street food of Old Delhi and signature kebabs crafted by master chefs." },
                new EventSeedDef { EventId = 10029, Title = "Pragati Maidan Auto Expo 2026", EventType = "Physical", DateTime = "2026-08-22T10:00:00Z", DurationHours = 8, VenueName = "Pragati Maidan Hall 5", RegionId = "REG06", Category = "Business", AgeCategory = "ALL", MinPrice = 200.00, ImageUrl = "https://images.unsplash.com/photo-1503376780353-7e6692767b70?w=500&auto=format&fit=crop&q=80", Description = "India's premier automobile expo featuring EV concepts and futuristic custom supercars." },
                new EventSeedDef { EventId = 10030, Title = "Qutub Minar Classical Music Fest", EventType = "Physical", DateTime = "2026-11-15T18:00:00Z", DurationHours = 4, VenueName = "Qutub Minar Complex Grounds", RegionId = "REG06", Category = "Music", AgeCategory = "ALL", MinPrice = 500.00, ImageUrl = "https://images.unsplash.com/photo-1514525253161-7a46d19cd819?w=500&auto=format&fit=crop&q=80", Description = "Classical Indian music festival backdropped by the illuminated magnificent Qutub Minar." },

                // Bengaluru
                new EventSeedDef { EventId = 10031, Title = "Bengaluru AI & Developer Conference", EventType = "Physical", DateTime = "2026-10-05T09:00:00Z", DurationHours = 7, VenueName = "Chinnaswamy Stadium Club Hall", RegionId = "REG07", Category = "Technology", AgeCategory = "ALL", MinPrice = 500.00, ImageUrl = "https://images.unsplash.com/photo-1540575467063-178a50c2df87?w=500&auto=format&fit=crop&q=80", Description = "Network with leading developers and explore the latest advancements in LLMs and deep learning." },
                new EventSeedDef { EventId = 10032, Title = "Lalbagh Flower Show & Art Exhibition", EventType = "Physical", DateTime = "2026-08-15T08:00:00Z", DurationHours = 9, VenueName = "Lalbagh Glass House", RegionId = "REG07", Category = "Arts", AgeCategory = "ALL", MinPrice = 80.00, ImageUrl = "https://images.unsplash.com/photo-1498837167922-ddd27525d352?w=500&auto=format&fit=crop&q=80", Description = "Witness floral structures of national heritage monuments and shop organic craft displays." },
                new EventSeedDef { EventId = 10033, Title = "Nandi Hills Sunrise Cycling Tour", EventType = "Physical", DateTime = "2026-09-10T05:00:00Z", DurationHours = 5, VenueName = "Nandi Hills Foothills Gate", RegionId = "REG07", Category = "Arts", AgeCategory = "ALL", MinPrice = 450.00, ImageUrl = "https://images.unsplash.com/photo-1485965120184-e220f721d03e?w=500&auto=format&fit=crop&q=80", Description = "An exhilarating morning cycling trip up the hills to watch the sunrise above the clouds." },

                // Hyderabad
                new EventSeedDef { EventId = 10034, Title = "Hyderabad Biryani & Sufi Night", EventType = "Physical", DateTime = "2026-10-25T18:30:00Z", DurationHours = 5, VenueName = "Shilpakala Vedika", RegionId = "REG08", Category = "Music", AgeCategory = "ALL", MinPrice = 350.00, ImageUrl = "https://images.unsplash.com/photo-1514525253161-7a46d19cd819?w=500&auto=format&fit=crop&q=80", Description = "Enjoy the rich flavors of Nizami Biryani accompanied by soulful, live Sufi musical renditions." },
                new EventSeedDef { EventId = 10035, Title = "HITEX Tech Hackathon & AI Summit", EventType = "Physical", DateTime = "2026-09-24T09:00:00Z", DurationHours = 24, VenueName = "HITEX Exhibition Centre", RegionId = "REG08", Category = "Technology", AgeCategory = "ALL", MinPrice = 150.00, ImageUrl = "https://images.unsplash.com/photo-1540575467063-178a50c2df87?w=500&auto=format&fit=crop&q=80", Description = "Assemble and build novel generative AI tools and showcase to top startup accelerators." },
                new EventSeedDef { EventId = 10036, Title = "Golconda Fort Sound & Light Show", EventType = "Physical", DateTime = "2026-12-05T18:00:00Z", DurationHours = 2, VenueName = "Golconda Fort Inner Court", RegionId = "REG08", Category = "Arts", AgeCategory = "ALL", MinPrice = 120.00, ImageUrl = "https://images.unsplash.com/photo-1608958416738-f9b8c0c45d3e?w=500&auto=format&fit=crop&q=80", Description = "A dramatic narration of the glorious history of the Qutb Shahi dynasty with beautiful projection lighting." },

                // Kochi
                new EventSeedDef { EventId = 10037, Title = "Kerala Backwaters Folk Dance Festival", EventType = "Physical", DateTime = "2026-11-12T17:00:00Z", DurationHours = 3, VenueName = "Bolgatty Palace Grounds", RegionId = "REG09", Category = "Arts", AgeCategory = "ALL", MinPrice = 250.00, ImageUrl = "https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=500&auto=format&fit=crop&q=80", Description = "Witness captivating traditional Kerala folk dances, including Kathakali and Mohiniyattam performances." },
                new EventSeedDef { EventId = 10038, Title = "Kochi-Muziris Biennale Art Showcase", EventType = "Physical", DateTime = "2026-12-25T10:00:00Z", DurationHours = 8, VenueName = "Aspinwall House Fort Kochi", RegionId = "REG09", Category = "Arts", AgeCategory = "ALL", MinPrice = 300.00, ImageUrl = "https://images.unsplash.com/photo-1460661419201-fd4cecdf8a8b?w=500&auto=format&fit=crop&q=80", Description = "The largest contemporary art exhibition in Asia presenting works from international creative artists." },
                new EventSeedDef { EventId = 10039, Title = "Fort Kochi Beach Sunset Carnival", EventType = "Physical", DateTime = "2026-10-18T16:00:00Z", DurationHours = 5, VenueName = "Fort Kochi Beach Walkway", RegionId = "REG09", Category = "Food & Drinks", AgeCategory = "ALL", MinPrice = 99.00, ImageUrl = "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=500&auto=format&fit=crop&q=80", Description = "Beachside food trucks, Kerala folk music jam sessions, and local souvenir popups at sunset." },

                // Kolkata
                new EventSeedDef { EventId = 10040, Title = "Kolkata Literature & Poetry Conclave", EventType = "Hybrid", DateTime = "2026-12-03T10:00:00Z", DurationHours = 6, VenueName = "Rabindra Sadan Hall", RegionId = "REG10", Category = "Arts", AgeCategory = "ALL", MinPrice = 100.00, ImageUrl = "https://images.unsplash.com/photo-1517245386807-bb43f82c33c4?w=500&auto=format&fit=crop&q=80", Description = "An intellectual dialogue bringing together poets, writers, and artists to celebrate art and literature." },
                new EventSeedDef { EventId = 10041, Title = "Kolkata Durga Puja Cultural Carnival", EventType = "Physical", DateTime = "2026-10-20T17:00:00Z", DurationHours = 6, VenueName = "Red Road Exhibition Boulevard", RegionId = "REG10", Category = "Arts", AgeCategory = "ALL", MinPrice = 150.00, ImageUrl = "https://images.unsplash.com/photo-1561089689-6ec5e37b463b?w=500&auto=format&fit=crop&q=80", Description = "Grand display of award-winning Durga idols, traditional dhunuchi dance, and festive beats." },
                new EventSeedDef { EventId = 10042, Title = "Kolkata Book Fair & Authors Summit", EventType = "Physical", DateTime = "2026-11-28T10:00:00Z", DurationHours = 8, VenueName = "Central Park Fair Ground Salt Lake", RegionId = "REG10", Category = "Business", AgeCategory = "ALL", MinPrice = 50.00, ImageUrl = "https://images.unsplash.com/photo-1506880018603-83d5b814b5a6?w=500&auto=format&fit=crop&q=80", Description = "A mega book fair featuring global publishers and panel talks by renowned literary novelists." },

                // Salem
                new EventSeedDef { EventId = 10043, Title = "Salem Mango Exhibition & Agri Expo", EventType = "Physical", DateTime = "2026-07-15T09:00:00Z", DurationHours = 10, VenueName = "Salem Corporation Exhibition Ground", RegionId = "REG14", Category = "Food & Drinks", AgeCategory = "ALL", MinPrice = 50.00, ImageUrl = "https://images.unsplash.com/photo-1498837167922-ddd27525d352?w=500&auto=format&fit=crop&q=80", Description = "Discover the agricultural diversity of Salem and taste organic mangoes directly from local farms." },
                new EventSeedDef { EventId = 10044, Title = "Yercaud Hills Coffee Estate Tour", EventType = "Physical", DateTime = "2026-09-20T08:00:00Z", DurationHours = 5, VenueName = "Yercaud Estate Viewpoint Cafe", RegionId = "REG14", Category = "Food & Drinks", AgeCategory = "ALL", MinPrice = 350.00, ImageUrl = "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=500&auto=format&fit=crop&q=80", Description = "A walking tour inside lush organic coffee estates to learn harvesting and coffee brewing." },
                new EventSeedDef { EventId = 10045, Title = "Salem Silk Saree Weaver Exhibition", EventType = "Physical", DateTime = "2026-10-25T10:00:00Z", DurationHours = 8, VenueName = "Salem Town Hall Grounds", RegionId = "REG14", Category = "Arts", AgeCategory = "ALL", MinPrice = 50.00, ImageUrl = "https://images.unsplash.com/photo-1460661419201-fd4cecdf8a8b?w=500&auto=format&fit=crop&q=80", Description = "Buy pure silk sarees directly from local master weavers at wholesale coop prices." },

                // Tirunelveli
                new EventSeedDef { EventId = 10046, Title = "Tirunelveli Halwa Tasting & Folk Night", EventType = "Physical", DateTime = "2026-08-20T18:00:00Z", DurationHours = 4, VenueName = "Nellai Exhibition Ground", RegionId = "REG15", Category = "Food & Drinks", AgeCategory = "ALL", MinPrice = 75.00, ImageUrl = "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=500&auto=format&fit=crop&q=80", Description = "Relish authentic, warm Tirunelveli Halwa while experiencing traditional folk performances by regional artists." },
                new EventSeedDef { EventId = 10047, Title = "Courtallam Waterfalls Cultural Fest", EventType = "Physical", DateTime = "2026-07-28T09:00:00Z", DurationHours = 6, VenueName = "Courtallam Main Falls Pavilion", RegionId = "REG15", Category = "Arts", AgeCategory = "ALL", MinPrice = 100.00, ImageUrl = "https://images.unsplash.com/photo-1470071459604-3b5ec3a7fe05?w=500&auto=format&fit=crop&q=80", Description = "Lakeside folk festival celebrating the monsoonal waterfalls with local arts and snacks." },
                new EventSeedDef { EventId = 10048, Title = "Nellaiappar Temple Chariot Festival Walk", EventType = "Physical", DateTime = "2026-09-08T06:00:00Z", DurationHours = 4, VenueName = "Nellaiappar Sannadhi Street", RegionId = "REG15", Category = "Arts", AgeCategory = "ALL", MinPrice = 50.00, ImageUrl = "https://images.unsplash.com/photo-1544735716-392fe2489ffa?w=500&auto=format&fit=crop&q=80", Description = "Guided photography and heritage tour surrounding the massive historical chariot drawing festival." }
            };
        }
        #endregion
    }
}
