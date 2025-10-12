using DCAS.Data;
using DCAS.Models;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using System;
using System.Diagnostics;
using System.Security.Claims;
using System.Security.Principal;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DCAS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DCASContext _context;

        public HomeController(ILogger<HomeController> logger, DCASContext context)
        {
            _logger = logger;
            _context = context;
        }

        public void ExistingTable()
        {
            var tableCheckQueryOne = @"
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PersonInfo')
    BEGIN
        CREATE TABLE PersonInfo (
            Id INT PRIMARY KEY IDENTITY(1,1) NOT NULL,
            Name NVARCHAR(255) NOT NULL,
            DetailOnee NVARCHAR(255) NOT NULL,
            HomeAddress NVARCHAR(255) NOT NULL,
            Date DATETIME NOT NULL,
            BirthDay DATETIME NOT NULL,
            Age INT NOT NULL,
            HomeNumber NVARCHAR(50) NOT NULL,
            Occupation NVARCHAR(100) NOT NULL,
            MobileNumber NVARCHAR(50) NOT NULL,
            OfficeAddress NVARCHAR(255) NOT NULL,
            EmailAddress NVARCHAR(255) NOT NULL,
            Status NVARCHAR(50) NOT NULL,
            NameOfSpouse NVARCHAR(255) NOT NULL,
            PersonalResponsibleforAccount NVARCHAR(255) NOT NULL,
            Relationship NVARCHAR(100) NOT NULL,
            DetailTwoo NVARCHAR(255) NOT NULL,
            PhysicianCare NVARCHAR(255) NOT NULL,
            PhysicianName NVARCHAR(255) NOT NULL,
            ContactNumber NVARCHAR(50) NOT NULL,
            MedicalServices NVARCHAR(250) NOT NULL,
            Price DECIMAL (10, 2) NOT NULL,
            DetailOne NVARCHAR(255) NOT NULL,
            DetailTwo NVARCHAR(255) NOT NULL,
            DetailThree NVARCHAR(255) NOT NULL,
            DetailFour NVARCHAR(255) NOT NULL,
            DetailFive NVARCHAR(255) NOT NULL,
            DetailSix NVARCHAR(255) NOT NULL,
            DetailSeven NVARCHAR(255) NOT NULL,
            AvailableDay NVARCHAR(50) NOT NULL,
            AvailableTime NVARCHAR(50) NOT NULL
        );
    END";

            _context.Database.ExecuteSqlRaw(tableCheckQueryOne);

            var tableCheckQueryTwo = @"
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TodaySchedule')
    BEGIN
        CREATE TABLE TodaySchedule (
            Id INT PRIMARY KEY IDENTITY(1,1) NOT NULL,
            EventDate DATETIME NOT NULL,
            EventTime TIME NOT NULL,
            PersonName NVARCHAR(255) NOT NULL
        );
    END";

            _context.Database.ExecuteSqlRaw(tableCheckQueryTwo);

            var tableCheckQueryServices = @"
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Services')
    BEGIN
        CREATE TABLE Services (
            Id INT PRIMARY KEY IDENTITY(1,1) NOT NULL,
            MedicalNameService NVARCHAR(255) NOT NULL,
            Price DECIMAL(10,2) NOT NULL
        );
    END";
            _context.Database.ExecuteSqlRaw(tableCheckQueryServices);

            var tableCheckQueryMedicineInventory = @"
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MedicineInventory')
    BEGIN
         CREATE TABLE MedicineInventory (
    Id INT PRIMARY KEY  identity(1,1),
    MedicineName VARCHAR(255) NOT NULL,
    Price Decimal(10,2) not null,
    Quantity INT NOT NULL,
    Miligram Nvarchar(Max),
    Description Nvarchar(Max),
    ExpiryDate DATE NOT NULL,
Image VARBINARY(MAX) 
        );
    END";
            _context.Database.ExecuteSqlRaw(tableCheckQueryMedicineInventory);

            var tableCheckQueryUsers = @"
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users')
    BEGIN
    CREATE TABLE Users (
    UsersId INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(255),
    Address NVARCHAR(255),
    Email NVARCHAR(255),
    PhoneNumber NVARCHAR(20),
    Role NVARCHAR(50),
    JobTitle NVARCHAR(50),
    Specialization NVARCHAR(50),
    Gender NVARCHAR(10),
    Nationality NVARCHAR(50),
    Position NVARCHAR(50),
    WorkStatus NVARCHAR(20),
    Age NVARCHAR(5),
    BirthDate DATETIME,
    StartDate DATETIME,
    Profile VARBINARY(MAX),
    Username NVARCHAR(50),
    Password NVARCHAR(255)
        );
    END";
            _context.Database.ExecuteSqlRaw(tableCheckQueryUsers);

            var tableCheckQueryPayment = @"
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Payments')
    BEGIN
      CREATE TABLE Payments(
    PaymentId INT PRIMARY KEY IDENTITY,       
    Cash DECIMAL(18, 2),                      
    DoctorName VARCHAR(255),                  
    Status VARCHAR(100),                      
    PaymentMethod VARCHAR(100),               
    AmountPaid DECIMAL(10, 2),                
    AmountChanged DECIMAL(10, 2),             
    DentistFee DECIMAL(10, 2),                
    PaymentDate DATE,                     
    PersonInfoId INT,                         
    ServicesId INT,                           
                           


    FOREIGN KEY(PersonInfoId) REFERENCES PersonInfo(Id),  
    FOREIGN KEY(ServicesId) REFERENCES Services(Id),      );
    END";
            _context.Database.ExecuteSqlRaw(tableCheckQueryPayment);

            var tableCheckQueryPaymentMedicine = @"
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PaymentMedicine')
    BEGIN
    CREATE TABLE PaymentMedicine (
    PaymentMedicineId INT PRIMARY KEY IDENTITY,
    PaymentId INT NOT NULL,
MedicineName       NVARCHAR(max) not null,
    Price DECIMAL(10, 2),
    
    FOREIGN KEY(PaymentId) REFERENCES Payments(PaymentId),
);
    END";
            _context.Database.ExecuteSqlRaw(tableCheckQueryPaymentMedicine);
        }



        [Authorize(Roles = "Admin,Staff")]
        public IActionResult Index()
        {
            ExistingTable();
            return View();
        }

        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Profile()
        {
            // Get the current user's username from the claims
            var username = User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(username))
            {
                return NotFound(); // No user found in claims
            }

            // Find the user in the database using the username
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (currentUser == null)
            {
                return NotFound(); // User not found in the database
            }

            return View("Profile", currentUser);
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
