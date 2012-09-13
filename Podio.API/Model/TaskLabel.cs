using System;
using System.Collections.Generic;
using System.Runtime.Serialization;


namespace Podio.API.Model
{
	[DataContract]
	public class TaskLabel 
	{


		[DataMember(Name = "label_id", IsRequired=false)]
		public int? LabelId { get; set; }


		[DataMember(Name = "text", IsRequired=false)]
		public string Text { get; set; }


		[DataMember(Name = "color", IsRequired=false)]
		public string Color { get; set; }


	}
}
