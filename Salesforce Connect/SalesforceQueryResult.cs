using UnityEngine;
using System.Collections;
using Boomlagoon.JSON;
using System.Collections.Generic;

namespace SalesforceConnect{

	public  partial class SalesforceQueryResult : MonoBehaviour {
		/*
			SalesforceQueryResult is nothing more than an array constructed from a JSON result. It is built using Boomlagoon's JSON library. 
			Salesforce interaction is handled via web service and the results always  come as JSONs. You are free to go to the library and see its implementation.
			This array contains result records represented as a Dictionary of field:value strings. You can not add or modify elements to it,
			just inspect the result of a query.
			
			Whenever you get a result, you can know the amount of records it possesses or you can know if a record with a given id is present.
			Additionally you can know what kind of object it holds.
			Then you can get the result object by index or by id;
		*/
		public int amountOfRecords;
		public string objectType;
		
		public Dictionary<string,string> GetRecordMapWithIndex(int index){
			
			if(isCompleted && (index < amountOfRecords )){
				Dictionary<string,string> result = new Dictionary<string,string>();
				foreach(string key in records[index].Obj.values.Keys ){
					result[key] = records[index].Obj.values[key].ToString();
				}
				return result;
			}
			return null;
		}
		
		public bool HasRecordWithID(string id){
			for(int i = 0; i< records.Length; i++ ){
				if(records[i].Obj.values.ContainsKey("Id") && records[i].Obj.values["Id"].ToString().Contains(id)){
					return true;
				}
			}
			return false;
		}
		
		public Dictionary<string,string> GetRecordMapById(string id){
			if(isCompleted){
				Dictionary<string,string> result = new Dictionary<string,string>();
				
				int recordIndex = -1;
				for(int i = 0; i< records.Length; i++ ){
					if(records[i].Obj.values.ContainsKey("Id") && records[i].Obj.values["Id"].ToString().Contains(id)){
						recordIndex = i;
						break;
					}
				}
				
				if(recordIndex != -1){
					foreach(string key in records[recordIndex].Obj.values.Keys ){
						result[key] = records[recordIndex].Obj.values[key].ToString();
					}
				}
				
				return result;
			}
			return null;
		}
		
	}
	
}