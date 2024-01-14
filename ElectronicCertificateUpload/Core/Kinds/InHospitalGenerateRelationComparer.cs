using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectronicCertificateUpload.Core
{
    public class InHospitalGenerateRelationComparer : IEqualityComparer<RelationKind>
    {
        public bool Equals(RelationKind x, RelationKind y)
        {
            if (Object.ReferenceEquals(x, y))
                return true;

            if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
                return false;

            return x.HIS_SYXH == y.HIS_SYXH && x.HIS_JSXH == y.HIS_JSXH;
        }

        public int GetHashCode(RelationKind relation)
        {
            if (Object.ReferenceEquals(relation, null))
                return 0;

            int hashRelationSYXH = relation.HIS_SYXH.GetHashCode();

            int hashRelationJSXH = relation.HIS_JSXH.GetHashCode();

            return hashRelationSYXH ^ hashRelationJSXH;
        }
    }
}
