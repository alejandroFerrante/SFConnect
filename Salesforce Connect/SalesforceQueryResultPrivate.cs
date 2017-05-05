using UnityEngine;
using System;
using System.Collections;
using System.Text;
using Boomlagoon.JSON;
using System.Collections.Generic;

namespace SalesforceConnect{

	public  partial class SalesforceQueryResult : MonoBehaviour {
		
		/*
			These are the aspects that where chosen to be left private, even though some of them are public variables or methods.
			As it was stated in the public part of the class, this object uses Boomlagoon JSON library. 
			The underlying array of SalesforceQueryResult is not a normal one, but a JSONArray from the library.
			
			SalesforceQueryResults are constructed (you are not meant to construct them yourself, just inspect them. that's why this remained private) 
			in two parts: first you set the objectType. Then, when the response is available, (remember this is may take some time) 
			you get the raw JSON response and use the parse function provided by the Boomlagoon JSON library to extract the values.
			When this happens, the construction is completed.
			
		*/
		
		private string raw;
		private JSONArray records;
		private bool isCompleted;
		
		public void setType(string typeOfObject){
			if(!isCompleted){objectType = typeOfObject;}
		}
		
		public void FillWith(string rawResult){
			if(!isCompleted){
				raw = rawResult;
				records = JSONObject.Parse(raw).GetArray("records");
				amountOfRecords = records.Length;
				isCompleted = true;
			}
		}
		
	}

}