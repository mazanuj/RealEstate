using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Caliburn.Micro.Validation;

namespace RealEstate.Validation
{
    /// <summary>
    /// Validates the entry of a single url address
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class UrlAttribute : RegularExpressionAttribute, IValidationControl
    {
        /// <summary>
        /// RegEx expression used
        /// </summary>
        public const string EmailValidationExpression = @"https{0,1}://.+\..+";

        /// <summary>
        /// Default constructor
        /// </summary>
        public UrlAttribute()
            : base(string.Format("^{0}$", EmailValidationExpression))
        { }

        #region IValidationControl

        /// <summary>
        /// When true a validation controller will 
        /// </summary>
        public bool ValidateWhileDisabled { get; set; }

        /// <summary>
        /// If not defined the guard property to check for disabled state is Can[PropertyName]
        /// However it may be necessary to test another guard property and this is the place 
        /// to specify the alternative property to query.
        /// </summary>
        public string GuardProperty { get; set; }

        #endregion
    }
}
