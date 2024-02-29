using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using UnderlyingApi.Models;




namespace UnderlyingApi.Data
{
    public class DataContext:DbContext
    {
                //Empty constructor
public DataContext(): base(){
}

//Database Connection String
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
base.OnConfiguring(optionsBuilder); 
optionsBuilder.UseSqlServer("Server=172.16.68.1,1433;Database=FingerPrintDb;User=sa;Password=HydotTech;TrustServerCertificate=true;");
}
//Data Set, where Project and User are models in the Model folder

public DbSet<TokenAndByte> TokenImage {get; set;}














    }
}