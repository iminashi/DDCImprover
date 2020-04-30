using Rocksmith2014Xml.Extensions;
using System;

namespace Rocksmith2014Xml
{
    public enum CommentType
    {
        Uninitialized,
        Toolkit,
        EOF,
        DDC,
        DDCImprover,
        Unknown
    }

    public sealed class RSXmlComment
    {
        private CommentType _commentType;

        public string Value { get; set; }

        public CommentType CommentType
        {
            get
            {
                if(!string.IsNullOrEmpty(Value) && _commentType == CommentType.Uninitialized)
                {
                    if (Value.IgnoreCaseContains("CST v"))
                        _commentType = CommentType.Toolkit;
                    else if (Value.IgnoreCaseContains("EOF"))
                        _commentType = CommentType.EOF;
                    else if (Value.IgnoreCaseContains("DDC Improver"))
                        _commentType = CommentType.DDCImprover;
                    else if (Value.IgnoreCaseContains("DDC v"))
                        _commentType = CommentType.DDC;
                    else
                        _commentType = CommentType.Unknown;
                }

                return _commentType;
            }
        }

        public RSXmlComment(string comment)
        {
            Value = comment;
        }

        public override string ToString() => Value;
    }
}