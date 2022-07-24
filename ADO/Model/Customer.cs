using System;


namespace ADO.Model
{
    public record Customer
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Location { get; set; }

        public string Email { get; set; }


        public Customer() { }
    }
}
