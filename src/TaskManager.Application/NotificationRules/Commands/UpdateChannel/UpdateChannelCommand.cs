using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskManager.Domain.Enums;
namespace TaskManager.Application.NotificationRules.Commands.UpdateChannel
{
    public sealed record UpdateChannelCommand
    (
        Guid RuleId,
        NotificationChannel Channel
    ) : I
}