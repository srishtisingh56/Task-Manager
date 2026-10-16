using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskManager.Application.Tasks.Common
{
    public enum TaskRelationFilter
    {
        AssignedToMe,
        //assigned by me to someone else
        AssignedByMe,
        Personal
    }
}