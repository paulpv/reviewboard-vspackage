using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace org.reviewboard.ReviewBoardVs
{
    public class ReviewInfo
    {
        public int Id { get; protected set; }
        public Uri Uri { get; protected set; }

        public ReviewInfo(int id, Uri uri)
        {
            Id = id;
            Uri = uri;
        }

        public override string ToString()
        {
            return new StringBuilder().Append(Id).Append(" - ").Append(Uri.AbsoluteUri).ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            ReviewInfo other = (ReviewInfo)obj;

            return Id.Equals(other.Id) && string.Compare(Uri.AbsoluteUri, other.Uri.AbsoluteUri, true) == 0;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() ^ Uri.GetHashCode();
        }
    }
}
