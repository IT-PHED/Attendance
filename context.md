# API's and Routes

Route::post('v1/attendance/upload', 'AttendanceController@upload')->name('store_attendance_image');
Route::post('v1/attendance/store', 'AttendanceController@store')->name('store_attendance');
Route::get('v1/my_attendance', [AttendanceController::class, 'my_attendance']);
Route::get('v1/office', 'OfficeController@apiDisplay')->name('get_offices');
Route::get('v1/getAStaff', 'OfficeController@getAStaff')->name('api.getAStaff');
Route::match(['get', 'post'], 'v1/get_staff_attendance', 'OfficeController@getStaffAttendance')->name('api.getStaffAttendance');

Route::get('/attendancereport', [AttendanceReportController::class, 'view'])->name('attendancereport');
  Route::get('/attendancereportabsent', [AttendanceReportController::class, 'getAbsentStaff'])->name('absentreport');
  Route::get('/attendancereportpunctual', [AttendanceReportController::class, 'getPunctualStaff'])->name('punctualreport');
  Route::get('/attendancereportlate', [AttendanceReportController::class, 'getLateStaff'])->name('latereport');
  Route::get('/attendancereportleave', [AttendanceReportController::class, 'getStaffOnLeave'])->name('leavereport');
  Route::get('/attendancereportnight', [AttendanceReportController::class, 'getNightStaff'])->name('nightreport');
  Route::get('/attendancereportstatistics', [AttendanceReportController::class, 'getStatistics'])->name('attendancestats');
  Route::get('/staff_attendance/{staff_id}/{from_date}/{to_date}', [AttendanceReportController::class, 'detailedView'])->name('staff_attendance');
  Route::get('/attendancereportvisit', [AttendanceReportController::class, 'getVisitStaff'])->name('visitreport');


# Attendance functions

public function upload(Request $request) 
  { 
  
   
    $validator = Validator::make($request->all(), [ 
        'file' => 'required|mimes:mp4,png,jpeg,txt,csv,mp3,jpg,hevc,mov|max:2048',
    ]);   

    if($validator->fails()) {          
        return response()->json(['error' => $validator->errors()], 401);                        
    }  

    if ($request->hasFile('file')) {
        $file = $request->file('file');
        $currentDate = Carbon::now()->toDateString(); // Format: YYYY-MM-DD
        $folderPath = 'public/files/' . $currentDate;

        if (!file_exists($folderPath)) {
            mkdir($folderPath, 0777, true); // Create directory recursively
        }
        
       $originalFilename = $file->getClientOriginalName();
		$timestamp = time(); // Get the current Unix timestamp
		$filename = $timestamp . '_' . $originalFilename;
		$file->move(public_path($folderPath), $filename); // Move the file to the public assets folder
		$path = $folderPath . '/' . $filename;


        return response()->json([
            "success" => true,
            "message" => "File successfully uploaded",
            "file" => $file,
            "path" => $path
        ]);
    
	}
 
  }

  public function store(Request $request){
    $lastAttendance = Attendance::where('staff', $request->id)
        ->orderBy('time', 'desc')
        ->first();
    if ($lastAttendance){ 
        $minsDiff = Carbon::parse($lastAttendance->time)->diffInMinutes(Carbon::parse($request->time), false);
        $hoursDiff = Carbon::parse($lastAttendance->time)->diffInHours(Carbon::parse($request->time), false);
        if (($minsDiff) > 0 && $hoursDiff <= 14){
            // Perform clock-out for previous day
           return  $this->clockOut($request);
        }
        else if ($minsDiff <= 0){
            return  $this->clockIn($request);
            \Log::info('Mins diff: ' . $minsDiff);
        } else {
            return  $this->updateClockIn($request, $lastAttendance);
        }
    } else{
        \Log::info('else');
        return $this->clockIn($request);
    }
  }

  public function updateClockIn(Request $request, $lastAttendance){
    $lastAttendance->timeout = $lastAttendance->timeout;
    $lastAttendance->time = $request->time;
    $lastAttendance->latitude= $request->lat;
    $lastAttendance->longitude = $request->long;
    $lastAttendance->image=$request->image;
    $lastAttendance->office_name = $request->office;
    if(strlen($request->name) <= 3){
        $lastAttendance->name = "N/A";
    }else{
        $lastAttendance->name = $request->name;
    }
    $lastAttendance->device_id = $request->device_id;
    $lastAttendance->save();
    return response()->json([
      "success" => true,
      "message" => "Attendance Updated successfully ",
      
    ]);
  }

  public function clockIn(Request $request){
        $attendance = new Attendance();
        $attendance->staff= $request->id;
        $attendance->time = $request->time;
        $attendance->latitude= $request->lat;
        $attendance->longitude = $request->long;
        $attendance->image=$request->image;
        $attendance->office_name = $request->office;
        if(strlen($request->name) <= 3){
            $attendance->name = "N/A";
        }else{
            $attendance->name = $request->name;
        }
        $attendance->device_id = $request->device_id;
        $attendance->save();
        return response()->json([
          "success" => true,
          "message" => "Attendance Saved successfully ",
          
        ]);
  }

  public function clockout(Request $request){

    if (!isset($request->day)) {
        // If day is not defined, fetch the latest attendance record for the staff
        $latestAttendance = Attendance::where('staff', $request->id)
            ->orderBy('time', 'desc')
            ->first();

        if ($latestAttendance) {
            // Update the timeout value for the latest attendance record
            $latestAttendance->timeout = $request->time;
            $latestAttendance->save();
            return response()->json([
                "success" => true,
                "message" => "Attendance saved successfully ",
            ]);
        } else {
            return response()->json([
                "success" => false,
                "message" => "No attendance record found for the staff.",
            ]);
        }
    } else {
        // If day is defined, fetch attendance records for the specified day and staff
        $attendanceRecord = Attendance::whereDate('time', $request->day)
            ->where('staff', $request->id)
            ->first();

        if ($attendanceRecord) {
            // Update the timeout value for the attendance record
            $attendanceRecord->timeout = $request->time;
            $attendanceRecord->save();
            return response()->json([
                "success" => true,
                "message" => "Attendance saved successfully ",
            ]);
        } else {
            return response()->json([
                "success" => false,
                "message" => "No attendance record found for the specified day and staff.",
            ]);
        }
    }
}

public function my_attendance(Request $request)
    {
        $today = Carbon::today()->toDateString();

        // Default to single day
        $from = $today;
        $to   = $today;

        // get 1 month history
        if ($request->boolean('history')) {
            $from = Carbon::today()->subDays(31)->toDateString();
            $to   = $today;
        }

        $my_attendance = Attendance::where('staff', $request->staff_id)
            ->whereBetween(DB::raw('DATE(time)'), [$from, $to])
            ->orderBy('time', 'desc')
            ->get();

        return response()->json($my_attendance, 200, [
            'Access-Control-Allow-Origin' => '*',
            'Access-Control-Allow-Methods' => 'GET, POST, OPTIONS',
            'Access-Control-Allow-Headers' => 'Content-Type, Authorization',
        ]);
    }


# Office and staff APIs

public function store(Request $request)
  {
      $office = Office();
      $office->name = $request->name;
      $office->address = $request->address;
      $office->latitude = $request->lat;
      $office->longitude = $request->long;
      $office->save();
  }

public function getAStaff(Request $request)
  {
    $user = User::select('email', 'name', 'staff_number')
      ->where('staff_number', $request->staff_number)  
      ->first();

    return response()->json($user)
        ->header('Access-Control-Allow-Origin', '*')
        ->header('Access-Control-Allow-Methods', 'GET, POST, OPTIONS')
        ->header('Access-Control-Allow-Headers', 'Content-Type, Authorization');
  }

public function getStaffAttendance(Request $request) 
  {
    \Log::info('Request received', [
        'method' => $request->method(),
        'data' => $request->all(),
        'headers' => $request->headers->all()
    ]);

    try {
        // Handle different HTTP methods
        $data = [];
        
        if ($request->isMethod('get')) {
            // For GET requests, get parameters from query string
            $data = [
                'from' => $request->query('from'),
                'to' => $request->query('to'),
                'staffId' => $request->query('staffId')
            ];
        } else {
            // For POST requests, get parameters from request body
            $data = [
                'from' => $request->input('from'),
                'to' => $request->input('to'),
                'staffId' => $request->input('staffId')
            ];
        }

        // Validate input
        $validated = validator($data, [
            'from' => 'required|date|date_format:Y-m-d',
            'to' => 'required|date|date_format:Y-m-d|after_or_equal:from',
            'staffId' => 'required|string'
        ])->validate();

        \Log::info('Validated parameters', $validated);

        // Call stored procedure
        $results = DB::select(
            'CALL GetStaffAttendance(?, ?, ?)', 
            [
                $validated['from'],
                $validated['to'],
                $validated['staffId']
            ]
        );
        
        \Log::info('Query executed successfully', ['result_count' => count($results)]);

        // Return successful response with CORS headers
        return response()->json([
            'success' => true,
            'data' => collect($results),
            'report_date' => now()->toDateString(),
            'parameters' => [
                'from' => $validated['from'],
                'to' => $validated['to'],
                'staffId' => $validated['staffId']
            ]
        ])
        ->header('Access-Control-Allow-Origin', '*')
        ->header('Access-Control-Allow-Methods', 'GET, POST, OPTIONS')
        ->header('Access-Control-Allow-Headers', 'Content-Type, Authorization, X-Requested-With')
        ->header('Access-Control-Max-Age', '86400');
        
    } catch (\Illuminate\Validation\ValidationException $e) {
        \Log::warning('Validation failed', [
            'errors' => $e->errors(),
            'input' => $data ?? []
        ]);
        
        return response()->json([
            'success' => false,
            'message' => 'Validation failed',
            'errors' => $e->errors()
        ], 422)
        ->header('Access-Control-Allow-Origin', '*')
        ->header('Access-Control-Allow-Methods', 'GET, POST, OPTIONS')
        ->header('Access-Control-Allow-Headers', 'Content-Type, Authorization, X-Requested-With');
        
    } catch (\Exception $e) {
        \Log::error('Error in getStaffAttendance', [
            'error' => $e->getMessage(),
            'trace' => $e->getTraceAsString(),
            'input' => $data ?? []
        ]);
        
        return response()->json([
            'success' => false,
            'message' => 'Server error occurred',
            'error' => env('APP_DEBUG') ? $e->getMessage() : 'Internal server error'
        ], 500)
        ->header('Access-Control-Allow-Origin', '*')
        ->header('Access-Control-Allow-Methods', 'GET, POST, OPTIONS')
        ->header('Access-Control-Allow-Headers', 'Content-Type, Authorization, X-Requested-With');
    }
  }

  public function apiDisplay(){
   $offices= Office::all(); 
   return response()->json($offices)
        ->header('Access-Control-Allow-Origin', '*')
        ->header('Access-Control-Allow-Methods', 'GET, POST, OPTIONS')
        ->header('Access-Control-Allow-Headers', 'Content-Type, Authorization');
  }

# Attendance Report APIs

class AttendanceReportController extends Controller
{
    public function __construct()
    {
        $this->middleware('auth');

        // redirect to home if not HCM or superadmin
        $this->middleware(function ($request, $next) {
            if (!in_array(Auth::user()->role, ['hcm', 'superadmin'])) {
                return redirect('dashboard')->with('error', 'You do not have permission to access this page.');
            }
    
            return $next($request);
        });
    }

    public function getStaffOnLeave(Request $date)
    {
        $results = DB::select('CALL LeaveStaffReport(?, ?)', [$date->input('from'), $date->input('to')]);
        
        return response()->json([
            'success' => true,
            'data' => collect($results),
            'report_date' => now()->toDateString(),
            'count' => count($results),
            'type' => 'leave'
        ]);
    }

    public function getLateStaff(Request $date)
    {
        $results = DB::select('CALL LateStaffReport(?, ?)', [$date->input('from'), $date->input('to')]);
        
        return response()->json([
            'success' => true,
            'data' => collect($results),
            'report_date' => now()->toDateString(),
            'count' => count($results),
            'type' => 'late'
        ]);
    }

    public function getPunctualStaff(Request $date)
    {
        $results = DB::select('CALL PunctualStaffReport(?, ?)', [$date->input('from'), $date->input('to')]);
        
        return response()->json([
            'success' => true,
            'data' => collect($results),
            'report_date' => now()->toDateString(),
            'count' => count($results),
            'type' => 'punctual'
        ]);
    }

    public function getAbsentStaff(Request $date)
    {
        $results = DB::select('CALL AbsentStaffReport(?)', [$date->input('date')]);
              
        return response()->json([
            'success' => true,
            'data' => collect($results),
            'report_date' => now()->toDateString(),
            'count' => count($results),
            'type' => 'absent'
        ]);
    }

    public function getNightStaff(Request $date)
    {
        $results = DB::select('CALL NightStaffReport(?, ?)', [$date->input('from'), $date->input('to')]);
              
        return response()->json([
            'success' => true,
            'data' => collect($results),
            'report_date' => now()->toDateString(),
            'count' => count($results),
            'type' => 'night'
        ]);
    }

    public function getVisitStaff(Request $date)
    {
        $results = DB::select('CALL VisitStaffReport(?, ?)', [$date->input('from'), $date->input('to')]);
              
        return response()->json([
            'success' => true,
            'data' => collect($results),
            'report_date' => now()->toDateString(),
            'count' => count($results),
            'type' => 'visit'
        ]);
    }

    public function getStatistics(Request $date)
    {
        $results = DB::select('CALL GetStaffAttendanceStats(?, ?)', [$date->input('from'), $date->input('to')]);
              
        return response()->json([
            'success' => true,
            'data' => collect($results),
            'report_date' => now()->toDateString(),
            'count' => count($results),
            'type' => 'stats'
        ]);
    }

    public function view()
    {
        // Define the date and terms for lateness and leave period GOZ
        $fromdate = Carbon::parse(now())->toDateString();
        $todate = $fromdate;
        $request = new Request();
        $request->merge([
            'from' => $fromdate,
            'to'   => $todate,
        ]);

        $request2 = new Request();
        $request2->merge([
            'date' => $fromdate,
        ]);

        // $date = $request->input('date');
        $leaves = $this->getStaffOnLeave($request);
        $leaveData = $leaves->getData();

        $absents = $this->getAbsentStaff($request2);
        $absentData = $absents->getData();

        $lates = $this->getLateStaff($request);
        $lateData = $lates->getData();
        
        $punctuals = $this->getPunctualStaff($request);
        $punctualData = $punctuals->getData();

        $nights = $this->getNightStaff($request);
        $nightData = $nights->getData();

        $stats = $this->getStatistics($request);
        $statsData = $stats->getData();

        $visit = $this->getVisitStaff($request);
        $visitData = $visit->getData();

        return view('attendance.report', [
            'leaveCount' => $leaveData->count,
            'staffOnLeave' => collect($leaveData->data),
            'absentCount' => $absentData->count,
            'absentStaff' => collect($absentData->data),
            'lateCount' => $lateData->count,
            'lateStaff' => collect($lateData->data),
            'punctualCount' => $punctualData->count,
            'punctualStaff' => collect($punctualData->data),
            'nightCount' => $nightData->count,
            'nightStaff' => collect($nightData->data),
            'statsCount' => $statsData->count,
            'stats' => collect($statsData->data),
            'visitCount' => $visitData->count,
            'visitStaff' => collect($visitData->data),
        ]);
    }

    public function detailedView($staff_id, $from_date, $to_date)
    {
        $from = Carbon::parse($from_date)->startOfDay();
        $to = Carbon::parse($to_date)->endOfDay();

        // Query the attendance records
        $attendanceRecords = Attendance::where('staff', $staff_id)
            ->whereBetween('time', [$from, $to])
            ->orderBy('time', 'asc')
            ->get();

        // Get the staff details
        $staff = User::where('staff_number', $staff_id)->first();

        return view('attendance.detailedView', [
            'records' => $attendanceRecords,
            'staff' => $staff,
            'from_date' => $from,
            'to_date' => $to
        ]);
    }
}


# Models

class User extends Authenticatable
{
    use Notifiable, HasRoles;

    /**
     * The attributes that are mass assignable.
     *
     * @var array
     */
    //protected $fillable = [
     //   'email', 'password',
    //];
	    protected $fillable = ['name', 'email', 'department', 'location', 'status', 'staff_number', 'updated_by', 'designation'];


    /**
     * The attributes that should be hidden for arrays.
     *
     * @var array
     */
    protected $hidden = [
        'password', 'remember_token', 'created_at','updated_at',
    ];

    /**
     * The attributes that should be cast to native types.
     *
     * @var array
     */
    protected $casts = [
        //'email_verified_at' => 'datetime',
    ];

    // public function setPasswordAttribute($password)
    // {
    //     $this->attributes['password'] = Hash::make($password);
    // }

  public function leave_request()
  {
    return $this->hasMany(LeaveRequest::class);
  }

  public function leave()
  {
    return StaffLeaveInfo::where('staff_number', $this->staff_number)->get();
  }


  public function avatar()
  {
    return $this->hasOne(Avatar::class);
  }

  public function f1_data()
  {
    return $this->hasOne(RegularStaff::class, 'staff_id', 'staff_number');
  }

  public function f2_data()
  {
    return $this->hasOne(RegularStaffModified::class, 'staff_id', 'staff_number');
  }

public function f3_data($staff_id)
  {
    return DB::table('new_hires_jan_mar_2021')
      ->where('staff_id', '=', $staff_id)
      ->first();
  }

  public function c1_data()
  {
    return $this->hasOne(ContractStaff::class, 'staff_id', 'staff_number');
  }

  public function c2_data()
  {
    return $this->hasOne(ContractStaffModified::class, 'staff_id', 'staff_number');
  }
}

class User extends Authenticatable
{
    use HasApiTokens, HasFactory, Notifiable;

    /**
     * The attributes that are mass assignable.
     *
     * @var string[]
     */
    //protected $fillable = [
    //    'name',
    //    'email',
    //    'password',
    //];

    /**
     * The attributes that should be hidden for serialization.
     *
     * @var array
     */
    protected $hidden = [
        'password',
        'remember_token',
    ];

    /**
     * The attributes that should be cast.
     *
     * @var array
     */
    protected $casts = [
        'email_verified_at' => 'datetime',
    ];
	 protected $fillable = ['name', 'email', 'department', 'location', 'status', 'staff_number', 'updated_by', 'designation'];

}


class Office extends Model
{
   // use HasFactory;
    protected $table = 'offices';
    protected $primaryKey = 'id';

}

class Attendance extends Model
{
  //  use HasFactory;
    protected $table = 'attendances';
    protected $primaryKey = 'id';
}


# db settings
DB_CONNECTION=mysql
DB_HOST=127.0.0.1
DB_PORT=3306
DB_DATABASE=hr
DB_USERNAME=root
DB_PASSWORD=jaja2020