using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TaskManager.Domain.Common;
using TaskManager.Domain.Exceptions;

namespace TaskManager.Domain.Entities
{
    public sealed class User : Entity
    {
        public string Name { get; private set; } = null!;
        public string Email { get; private set; } = null!;
        public string PhoneNumber { get; private set; } = null!;
        public string PasswordHash { get; private set; } = null!;

        //It exists to catch obviously malformed input at the domain boundary.
        private static readonly Regex EmailPattern =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        //ManagerId can be null meaning this user has no manager,assigning task to itself is allowed.
        public Guid? ManagerId { get; private set; }

        //Unrelated to the manager hierarchy. Grants full read access to all users/tasks
        public bool IsSystemAdmin { get; private set; }

        // User entity — mirrors TaskItem's soft-delete shape
        public bool IsActive { get; private set; } = true;

        public void Deactivate()
        {
            IsActive = false;
        }

        public void Reactivate()
        {
            IsActive = true;
        }
        private User()
        {}

        private User(Guid id,string name, string email, string phoneNumber, string passwordHash)
        : base(id)
        {
            Name = name;
            Email = email;
            PhoneNumber = phoneNumber;
            PasswordHash = passwordHash;
            IsSystemAdmin = false;
            ManagerId = null;
        }

        public static User Create(string name, string email, string phoneNumber, string passwordHash)
        {
            name = name?.Trim() ?? string.Empty;
            email = email?.Trim() ?? string.Empty;
            phoneNumber = phoneNumber?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty.", nameof(name));

            if (string.IsNullOrWhiteSpace(email) || !EmailPattern.IsMatch(email))
                throw new ArgumentException("Invalid email format.", nameof(email));

            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Phone number cannot be empty.", nameof(phoneNumber));

            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("Password hash cannot be empty.", nameof(passwordHash));

            return new User(Guid.NewGuid(), name, email, phoneNumber, passwordHash);
        }

        // passwordHash must already be hashed by the caller (Application/Infrastructure) — Domain never sees plaintext.
        public void SetPassword(string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("Password hash cannot be empty.", nameof(passwordHash));

            PasswordHash = passwordHash;
        }

        public void UpdateContactDetails(string name, string email, string phoneNumber)
        {
            name = name?.Trim() ?? string.Empty;
            email = email?.Trim() ?? string.Empty;
            phoneNumber = phoneNumber?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty.", nameof(name));

            if (string.IsNullOrWhiteSpace(email) || !EmailPattern.IsMatch(email))
                throw new ArgumentException("Invalid email format.", nameof(email));

            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Phone number cannot be empty.", nameof(phoneNumber));

            Name = name;
            Email = email;
            PhoneNumber = phoneNumber;
        }

        public void AssignManager(User manager)
        {
            ArgumentNullException.ThrowIfNull(manager);

            if (manager.Id == Id)
            {
                throw new SelfManagementException(Id);
            }

            ManagerId = manager.Id;
        }

        public void RemoveManager()
        {
            ManagerId = null;
        }

        public void PromoteToSystemAdmin()
        {
            IsSystemAdmin = true;
        }

        public void DemoteFromSystemAdmin()
        {
            IsSystemAdmin = false;
        }

        public bool IsDirectManagerOf(User worker)
        {
            ArgumentNullException.ThrowIfNull(worker);
            return worker.ManagerId == Id;
        }
    }
}