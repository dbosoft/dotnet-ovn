using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dbosoft.OVN.Model.OVN;
using LanguageExt;
using LanguageExt.Common;

namespace Dbosoft.OVN;

public class ChassisPlanRealizer : PlanRealizer
{
    private readonly IOVSDBTool _ovnDBTool;
    private readonly ILogger _logger;

    public ChassisPlanRealizer(IOVSDBTool ovnDBTool, ILogger logger) : base(ovnDBTool, logger)
    {
        _ovnDBTool = ovnDBTool;
        _logger = logger;
    }

    public EitherAsync<Error, ChassisPlan> ApplyChassisPlan(
        ChassisPlan chassisPlan,
        CancellationToken cancellationToken = default) =>
        Error.New("Peng!");
}
