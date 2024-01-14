using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectronicCertificateUpload.Core
{
    public class OutPatientGenerateRelationComparer : IEqualityComparer<RelationKind>
    {
        public bool Equals(RelationKind x, RelationKind y)
        {
            if (Object.ReferenceEquals(x, y))
                return true;

            if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
                return false;

            return x.HIS_GHXH == y.HIS_GHXH && x.HIS_JSSJH == y.HIS_JSSJH;
        }

        public int GetHashCode(RelationKind relation)
        {
            if (Object.ReferenceEquals(relation, null))
                return 0;

            int hashRelationGHXH = relation.HIS_GHXH.GetHashCode();

            int hashRelationJSSJH = relation.HIS_JSSJH == null ? 0 : relation.HIS_JSSJH.GetHashCode();

            return hashRelationGHXH ^ hashRelationJSSJH;
        }
    }
}
