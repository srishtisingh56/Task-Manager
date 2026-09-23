using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Validators;

namespace TaskManager.Application.Users.Commands.UpdateContactDetails
{
    public sealed class UpdateContactDetailsCommandValidator : AbstractValidator<UpdateContactDetailsCommand>
    {
        public UpdateContactDetailsCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();

            When(x => x.Name.IsSet, () =>
                RuleFor(x => x.Name.Value)
                .NotEmpty().WithMessage("Name must not be empty")
                .MaximumLength(200).WithMessage("Name must not exceed 200 characters"));   

            When(x => x.Email.IsSet, () =>
                RuleFor(x => x.Email.Value)
                .NotEmpty().WithMessage("Email must not be empty")
                .EmailAddress().WithMessage("Email must be a valid email address")
                .MaximumLength(320).WithMessage("Email must not exceed 320 characters"));

            When(x => x.PhoneNumber.IsSet, () =>
                RuleFor(x => x.PhoneNumber.Value)
                .NotEmpty().WithMessage("Phone number must not be empty")
                .MaximumLength(10).WithMessage("Phone number must not exceed 10 characters"));
        }
    }
  
}