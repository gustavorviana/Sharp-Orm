using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder
{
    public struct TableName
    {
        public string Name { get; set; }
        public string Alias { get; set; }

        public TableName(string name, string alias)
        {
            this.Name = name;
            this.Alias = alias;
        }

        public TableName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                throw new ArgumentNullException(nameof(fullName));

            var splits = fullName.Replace(" as ", "").Replace(" AS ", "").Split(' ');
            if (splits.Length > 3)
                throw new ArgumentException("Table name is invalid.");

            this.Name = splits[0];
            this.Alias = splits.Length == 2 ? splits[1] : null;
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(this.Alias) ? this.Name : this.Alias;
        }
    }
}