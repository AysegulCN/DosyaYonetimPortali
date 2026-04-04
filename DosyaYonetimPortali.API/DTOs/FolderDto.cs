namespace DosyaYonetimPortali.API.DTOs
{
    public class FolderDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? ParentFolderId { get; set; }

        public List<FolderDto>? SubFolders { get; set; }
        public List<AppFileDto>? Files { get; set; }
    }
}