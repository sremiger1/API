# Making Request to the API Examples
If wanting to seed the database there is a method in the controller constructor to Seed the database with 2 records `_popsicleService.Seed()`. Uncomment the line and everytime the controller is constructed it will add 2 records to the database. 

## Get Request
*httpget*
	
	- https://localhost:7212/api/Popsicle/1 Returns Product if seeded
	- https://localhost:7212/api/Popsicle/-1  Returns 400 the application assumes all PKs are positive numbers.
	- https://localhost:7212/api/Popsicle/100 Returns not found 

## Create Request
*httppost*
	
	- Successful create 
		- URL https://localhost:7212/api/Popsicle
		- Body: 
		{"id": 0, "sku": "SKU10", "quantity": 10, "name": "SKU10 Name", "description":   "SKU10 Description", "color": "SKU10 Color", "ingredients": "SKU10 Ingredients"}
		- Returns: 200, and product

	- Failure Duplicate SKU
		- URL https://localhost:7212/api/Popsicle
		- Body: 
		{"id": 0,"sku": "SKU10",  "quantity": 10, "name": "SKU10 Name Part 2", "description": "SKU10 Description Part 2","color": "SKU10 Color Part 2","ingredients": "SKU10 Ingredients Part 2"}
		- Returns: 400 with error messages

	- Failure Multiple Errors
		- URL https://localhost:7212/api/Popsicle
		- Body: 
    	{"id": 0,"sku": "","quantity": 0,"name": "","description": "SKU11 Description","color": "SKU10 Color Part 2","ingredients": "SKU11 Ingredients"}
    	- Returns: 400 with error messages

## Search Request
*httpget*
 	
	- Successful Search https://localhost:7212/api/Popsicle?searchValue=KU should return 200 and at least 2 records depending on seeding.
 	- No Content Search https://localhost:7212/api/Popsicle?searchValue=XX should return 204 and no records
 	- Bad Request https://localhost:7212/api/Popsicle?searchValue= should return a 400 status search value is required

## Update
*httpput*

	- Successful update of an existing record return a 200
		- URL: https://localhost:7212/api/Popsicle/1
		- Body:
		{ "id": 1, "sku": "GREENSKU", "quantity": 0, "name": "GReen Name", "description": "Green Description", "color": "Green Color", "ingredients": "Green Ingredients"}
		- Returns: 200 and product

	- Failed missing SKU returns 400
		- URL: https://localhost:7212/api/Popsicle/1
		- Body: 
		{ "id": 0, "sku": "", "quantity": 0, "name": "Green Name", "description": "Green 			Description", "color": "Green Color", "ingredients": "Green Ingredients" }
		- Returns status 400 with error messages

	- Successful create returns 201
		- URL: https://localhost:7212/api/Popsicle/1100
		- Body: 
    			{"id": 0,"sku": "OrangeSKU1","quantity": 10,"name": "Orange Name",
      "description": "Blue Description","color": "Blue Color","ingredients": "Blue Ingredients"}
    - Returns: 201 and a product

## Delete Request
*httpdelete*

	- Successful delete returns 200
		- URL: https://localhost:7212/api/Popsicle/18 *May need to find a Primary Key*
		- Returns: 200
		- No other status is returned for security reasons.

## Patch Request
*httppatch*
	
	- Successful patch returns 200
		- URL: https://localhost:7212/api/Popsicle/1
		- Body:
    	[
    		{"value": "PATCHSKU","path": "/SKU","op": "replace"},
    		{"value": "PATCH Description","path": "/Description","op": "replace"}
    	]
		- Returns: 200 and the updated product