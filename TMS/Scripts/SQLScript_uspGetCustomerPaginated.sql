IF OBJECT_ID('dbo.uspGetCustomerPaginated', 'P') IS NOT NULL
   DROP PROCEDURE dbo.uspGetCustomerPaginated;
GO

CREATE PROCEDURE uspGetCustomerPaginated
(
    /* Optional Filters for Dynamic Search*/
	/* The following one parameter is optional*/
	/* Calling program uses this parameter variable to pass in @lessonTypeName='T'
	     to do a partial search*/
  @customerName NVARCHAR(100) = NULL,
    /*The following four parameter variables are often fixed for any stored procedure 
      which supports record paging. If no argument is provided for @pageNo 
	  (e.g. @pageNo=1 is missing), the default value is 1 */
  @pageNo INT = 1,   
  @pageSize INT = 10,
   /*– Sorting Parameters */
   /*-- If no argument is provided for @sortColumn parameter
          For example, @sortColumn='DONEAT' is missing, the default value is 'DEADLINE' */
   @sortColumn NVARCHAR(20) = 'ACCOUNTNAME',
   @sortOrder NVARCHAR(4)='ASC'
)
AS
BEGIN
    /*Declaring local variables which correspond to input parameters */
	/*The following @_pageNumber, @_pageSize, @_sortCol,
	    @_firstRec, @_lastRec and @_totalRows are used as part of the 
		SQL logic to support fetching the correct records which fit into the 
		specified page number. These variables are fixed for all stored procedures
		which support record paging. */
    DECLARE 
    @_customerName NVARCHAR(100),
    @_pageNumber INT,
    @_pageSize INT,
    @_sortCol NVARCHAR(20),
    @_firstRec INT,
    @_lastRec INT,
    @_totalRows INT

    /*Setting local Variables*/
	/*Copying all parameter input values into the respective local variables */
    SET @_customerName = LTRIM(RTRIM(@customerName))

	/*The following 6 commands rarely changed unless I encounter new inspiration
	   shared by good developers' articles and video */
	/*These 6 commands are used to calculate the correct set of records that can fit
	    into the specified page number (e.g. page 1, page 2 etc). 
		Without the correct value for the @_firstRec and @lastRec, the SQL logic will 
		fail to retrieve the correct page of records */
    SET @_pageNumber = @pageNo
    SET @_pageSize = @pageSize
    SET @_sortCol = LTRIM(RTRIM(@sortColumn))

    SET @_firstRec = ( @_pageNumber - 1 ) * @_pageSize
    SET @_lastRec = ( @_pageNumber * @_pageSize + 1 )
    --SET @_totalRows = @_firstRec - @_lastRec + 1
	SET @_totalRows = @_lastRec - @_firstRec - 1
	PRINT(@_firstRec)
	PRINT(@_lastRec)
	PRINT(@_totalRows)

    ;WITH CTE_Results
    AS (
    SELECT ROW_NUMBER() OVER (ORDER BY
      CASE WHEN (@_sortCol = 'ACCOUNTNAME' AND @sortOrder='ASC')
                THEN CA.AccountName
      END ASC,
      CASE WHEN @_sortCol = 'ACCOUNTNAME' AND @sortOrder='DESC'
              THEN CA.AccountName
      END DESC,
      CASE WHEN (@_sortCol = 'ISVISIBLE' AND @sortOrder='ASC')
              THEN IsVisible
      END ASC,
      CASE WHEN @_sortCol = 'ISVISIBLE' AND @sortOrder='DESC'
                THEN IsVisible
      END DESC
  ) AS ROWNUM,
  Count(*) over () AS TotalCount,
  CA.CustomerAccountId, CA.AccountName, CA.IsVisible, AU1.FullName AS 'CreatedBy', AU2.FullName AS 'UpdatedBy', CA.UpdatedAt FROM CustomerAccount CA, AppUser AU1, AppUser AU2  
  WHERE CA.CreatedById = AU1.Id AND CA.UpdatedById = AU2.Id AND
    (
	   @_customerName IS NULL OR CA.AccountName LIKE '%' + @_customerName + '%')
    )

SELECT
  TotalCount,
  ROWNUM,
  CustomerAccountId,
  AccountName,
  IsVisible,
  CreatedBy,
  UpdatedBy,
  UpdatedAt
FROM CTE_Results 
WHERE ROWNUM > @_firstRec AND ROWNUM < @_lastRec
      ORDER BY ROWNUM ASC

END
GO