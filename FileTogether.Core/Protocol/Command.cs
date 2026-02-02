namespace FileTogether.Core.Protocol;

public enum Command : byte
{
    // Client -> Server requests
    LIST = 1,      // Yêu cầu danh sách file
    UPLOAD = 2,    // Gửi file lên
    DOWNLOAD = 3,  // Tải file về
    DELETE = 4,    // Xóa file
        
    // Server -> Client responses
    OK = 10,       // Thành công
    ERROR = 11,    // Lỗi
    FILE_LIST = 12 // Trả về danh sách file
}