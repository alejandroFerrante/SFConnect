using UnityEngine;
using System.Collections;
using SalesforceConnect;//NOTICE THIS
using System.Collections.Generic;

/*
	This asumes session is already logged
*/
public class SimpleQueryExample : MonoBehaviour {
	
	public SalesforceSession sfController;//Reference via editor the gameObject that holds the SalesforceSession script
	
	public void DoSampleQuey(){
		
		List<string> fields = new List<string>();
		fields.Add("Id");
		fields.Add("name");
		
		if(sfController.isLogged()){
			sfController.QueryForAllRecordsOfType("Account", fields, true);
		}else{
			Debug.Log("YOU ARE NOT YET LOGGED!");
		}
		
	}
	
	public void InspectRandomRecord(){
		if(sfController.hasQuery()){
			SalesforceQueryResult queryResult = sfController.getLastQueryResult();
			int amountOfObjectsInQuery = queryResult.amountOfRecords;
			Dictionary<string,string> someRecord = queryResult.GetRecordMapWithIndex(Random.Range(0,amountOfObjectsInQuery));
			Debug.Log("RANDOML SELECTED RECORD: "+someRecord["Name"]);
		}else{
			Debug.Log("THERE's NO QUERY RESULT!");
		}
	}

}
