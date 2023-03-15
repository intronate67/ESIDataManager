using ECT.Models.ESI.Alliance;
using EsiDataManager.Models.ESI.Corporation;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ESIDataManager.Interfaces
{
    [Headers("User-Agent: EsiDataManager")]
    public interface IEveOnlineApi
    {
        [Get("/alliances/")]
        Task<long[]> GetAlliances(CancellationToken cancellationToken);
        [Get("/alliances/{id}/")]
        Task<Alliance> GetAlliance(long id, CancellationToken cancellationToken);
        [Get("/corporations/npccorps/")]
        Task<long[]> GetNpcCorporations(CancellationToken cancellationToken);
        [Get("/corporations/{id}/")]
        Task<Corporation> GetCorporation(long id, CancellationToken cancellationToken);
    }
}
