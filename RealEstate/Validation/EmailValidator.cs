﻿using System;
using System.ComponentModel.DataAnnotations;

namespace RealEstate.Validation
{
	/// <summary>
	/// Validates the entry of a single email address
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class EmailValidator : RegularExpressionAttribute, IValidationControl
	{
		/// <summary>
		/// RegEx expression used
		/// </summary>
		private const string EmailValidationExpression = @"([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)";

		/// <summary>
		/// Default constructor
		/// </summary>
		public EmailValidator() 
			: base(string.Format("^{0}$", EmailValidationExpression))
					/* "^[a-z0-9_\\+-]+(\\.[a-z0-9+-]+)*@[a-z0-9-]+(\\.[a-z0-9-]+)*\\.([a-z]{2,4})$" */
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
