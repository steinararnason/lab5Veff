﻿using System;
using System.Collections.Generic;
using System.Linq;
using CoursesAPI.Models;
using CoursesAPI.Services.DataAccess;
using CoursesAPI.Services.Exceptions;
using CoursesAPI.Services.Models.Entities;

namespace CoursesAPI.Services.Services
{
	public class CoursesServiceProvider
	{
		private readonly IUnitOfWork _uow;

		private readonly IRepository<CourseInstance> _courseInstances;
		private readonly IRepository<TeacherRegistration> _teacherRegistrations;
		private readonly IRepository<CourseTemplate> _courseTemplates; 
		private readonly IRepository<Person> _persons;

		private const int numberOnPage = 10;

		public CoursesServiceProvider(IUnitOfWork uow)
		{
			_uow = uow;

			_courseInstances      = _uow.GetRepository<CourseInstance>();
			_courseTemplates      = _uow.GetRepository<CourseTemplate>();
			_teacherRegistrations = _uow.GetRepository<TeacherRegistration>();
			_persons              = _uow.GetRepository<Person>();
		}

		/// <summary>
		/// You should implement this function, such that all tests will pass.
		/// </summary>
		/// <param name="courseInstanceID">The ID of the course instance which the teacher will be registered to.</param>
		/// <param name="model">The data which indicates which person should be added as a teacher, and in what role.</param>
		/// <returns>Should return basic information about the person.</returns>
		public PersonDTO AddTeacherToCourse(int courseInstanceID, AddTeacherViewModel model)
		{
			var course = _courseInstances.All().SingleOrDefault(x => x.ID == courseInstanceID);
			if (course == null)
			{
				throw new AppObjectNotFoundException(ErrorCodes.INVALID_COURSEINSTANCEID);
			}
			var teacher = _persons.All().SingleOrDefault(x => x.SSN == model.SSN);
			if (teacher == null)
			{
				throw  new AppObjectNotFoundException(ErrorCodes.TEACHER_IS_NOT_FOUND_IN_PERSON);
			}
			var alreadyHasMain =
				_teacherRegistrations.All().SingleOrDefault(x => x.ID == courseInstanceID && x.Type == TeacherType.MainTeacher);
			if (alreadyHasMain != null)
			{
				throw new AppObjectNotFoundException(ErrorCodes.COURSE_ALREADY_HAS_A_MAIN_TEACHER);
			}
			var adding = new TeacherRegistration
			{
				Type = model.Type,
				CourseInstanceID = courseInstanceID,
				SSN = model.SSN
			};
			_teacherRegistrations.Add(adding);
			_uow.Save();
			// TODO: implement this logic!
			return new PersonDTO
			{
				Name = teacher.Name,
				SSN = teacher.SSN
			};
		}

		public CourseInstanceDTO GetCourseById(int id)
		{
			var result = _courseInstances.All().SingleOrDefault(x => x.ID == id);
			if (result == null)
			{
				throw new AppObjectNotFoundException(ErrorCodes.INVALID_COURSEINSTANCEID);
			}
			var main = _teacherRegistrations.All().FirstOrDefault(x => x.ID == id && x.Type == TeacherType.MainTeacher);
			return new CourseInstanceDTO
			{
				CourseInstanceID = result.ID,
				TemplateID = result.CourseID,
				MainTeacher = main == null ? "" : _persons.All().SingleOrDefault(x => x.SSN == main.SSN).Name,
				Name = _courseTemplates.All().SingleOrDefault(x => x.CourseID == result.CourseID).Name
			};
		}

		/// <summary>
		/// You should write tests for this function. You will also need to
		/// modify it, such that it will correctly return the name of the main
		/// teacher of each course.
		/// </summary>
		/// <param name="semester"></param>
		/// <param name="page">1-based index of the requested page.</param>
		/// <returns></returns>
		public Envelope GetCourseInstancesBySemester(string semester = null, int page = 1, bool english = false)
		{
			if (string.IsNullOrEmpty(semester))
			{
				semester = "20153";
			}
			int skip = page - 1;
			skip *= numberOnPage;
			int pagenum = page*numberOnPage;
			var totalNumber = _courseInstances.All().Count();
			var courses = (from c in _courseInstances.All()
				join ct in _courseTemplates.All() on c.CourseID equals ct.CourseID
				where c.SemesterID == semester
				select new CourseInstanceDTO
				{
					Name = english ? ct.NameEN : ct.Name,
					TemplateID = ct.CourseID,
					CourseInstanceID = c.ID,
					MainTeacher = "" // Hint: it should not always return an empty string!
				}).ToList().Skip(skip).Take(pagenum).ToList();
			var pageCount = Math.Ceiling(((decimal)totalNumber/(decimal)numberOnPage));
			Envelope myEnvelope = new Envelope();
			myEnvelope.page = new Page();
			myEnvelope.courses = courses;
			myEnvelope.page.PageCount = (int) pageCount;
			myEnvelope.page.PageNumber = page;
			myEnvelope.page.PageSize = numberOnPage;
			myEnvelope.page.TotalNumberOfItems = totalNumber;
			return myEnvelope;
		}
	}
}
